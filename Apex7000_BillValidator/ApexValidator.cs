namespace PyramidNETRS232
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using log4net;
    using PTI.Serial;
    using Serial;


    /// <summary>
    ///     The main class that does the actual "talking" the acceptor. In the context of documentation,
    ///     this object what is referred to as the master and the acceptor is the slave device.
    /// </summary>
    public partial class PyramidAcceptor : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly object _mutex = new object();

        // Time at which escrow starts
        private DateTime _escrowStart = DateTime.MinValue;

        /// <summary>
        ///     Stores the prior enable/disable pattern, used with the Enable/Disable calls
        /// </summary>
        private byte _lastEnablePattern;


        private ICommPort _port;
        private bool _resetRequested;

        /// <summary>
        ///     Creates a new PyramidAcceptor using the specified configuration
        /// </summary>
        /// <param name="config">Operating RS-232 parameters</param>
        public PyramidAcceptor(RS232Config config)
        {
            Config = config;
            _lastEnablePattern = config.EnableMask;
        }


        // State variables for tracking between events and states
        private byte Ack { get; set; }

        // Track if we have already reported the cashbox state. We always
        // raise the cashbox missing event but report cashbox attached event
        // only once.
        private bool CashboxPresent { get; set; }

        // If true, the slave is reporting that a note is in escrow
        private bool NoteIsEscrowed { get; set; }

        // Last reported credit from slave
        private byte Credit { get; set; }

        // Additional feature: async flag to tell the slave
        // to ACCEPT or REJECT the note next time the master polls
        private EscrowCommands EscrowCommand { get; set; }

        // Used in case we need to retransmit
        private byte[] PreviouslySentMasterMsg { get; set; }

        /// <summary>
        ///     Slave's last state
        /// </summary>
        public States PreviousState { get; private set; }

        /// <summary>
        ///     Slave's last events
        /// </summary>
        public Events PreviousEvents { get; private set; }

        /// <summary>
        ///     Returns true if the communication thread is running normally
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        ///     Gets or sets the RS232 Configuration
        /// </summary>
        public RS232Config Config { get; set; }

        /// <summary>
        ///     Gets the current pause state. If the acceptor
        ///     is running and at least 1 bill is enabled, the acceptor
        ///     is not paused. Otherwise, the acceptor is considered
        ///     paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        ///     Releases comm port and related managed resources.
        /// </summary>
        public void Dispose()
        {
            _port?.Dispose();
        }


        /// <summary>
        ///     Returns a list of all available ports
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAvailablePorts()
        {
            return StrongPort.GetAvailablePorts();
        }


        /// <summary>
        ///     Stop talking to the slave and release the underlying comm port.
        ///     <remarks>Do not use this to disable the bill acceptor: use PauseAcceptance()</remarks>
        /// </summary>
        public void Close()
        {
            // This will kill the comms loop
            IsRunning = false;

            _port?.Disconnect();

            lock (_mutex)
            {
                IsPaused = true;
            }
        }

        /// <summary>
        ///     Disables the bill acceptor within the time period defined by the poll rate.
        ///     The poll rate (RS232Config.PollRate, default 50 ms) is the maximum time
        ///     between poll packets from master to slave. This command does not disconnect
        ///     the serial port. Use Close() for that effect.
        ///     This effectively tells the acceptor to stop accepting bill but keep reporting status.
        ///     The acceptor's lights will turn off after this call takes effect.
        ///     <seealso cref="ResumeAcceptance" />
        /// </summary>
        public void PauseAcceptance()
        {
            _lastEnablePattern = Config.EnableMask;
            Config.EnableMask = 0;

            lock (_mutex)
            {
                IsPaused = true;
            }
        }

        /// <summary>
        ///     Returns the acceptor to bill accepting mode. This command
        ///     has no effect if the acceptor is already running and accepting.
        ///     The acceptor's lights will turn on after this command takes effect.
        ///     The command will take up to Config.PollRate ms to take effect.
        ///     <seealso cref="PauseAcceptance" />
        /// </summary>
        [Obsolete("Use ResumeAcceptance instead (spelled correctly)")]
        public void ResmeAcceptance()
        {
            Config.EnableMask = _lastEnablePattern;

            lock (_mutex)
            {
                IsPaused = false;
            }
        }

        /// <summary>
        ///     Returns the acceptor to bill accepting mode. This command
        ///     has no effect if the acceptor is already running and accepting.
        ///     The acceptor's lights will turn on after this command takes effect.
        ///     The command will take up to Config.PollRate ms to take effect.
        ///     <seealso cref="PauseAcceptance" />
        /// </summary>
        public void ResumeAcceptance()
        {
            Config.EnableMask = _lastEnablePattern;

            lock (_mutex)
            {
                IsPaused = false;
            }
        }

        /// <summary>
        ///     Connect to the device and begin speaking rs232
        /// </summary>
        public void Connect()
        {
            // Lock so we only have one connection attempt at a time. This protects
            // from client code behaving badly.
            lock (_mutex)
            {
                try
                {
                    _port = Config.GetCommPort();
                }
                catch (IOException)
                {
                    NotifyError(Errors.FailedToOpenPort);
                    return;
                }

                _port.ReadTimeout = 500;

                DebugBufferEntry.SetEpoch();

                try
                {
                    _port.Connect();


                    // Only start if we connect without error
                    StartRS232Loop();

                    IsPaused = false;
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Exception Connecting to acceptor: {0}", e.Message, e);


                    if (OnError != null)
                    {
                        NotifyError(Errors.PortError);
                    }
                }
            }
        }

        /// <summary>
        ///     Sets flag to reset the acceptor on the next message sent
        /// </summary>
        public void RequestReset()
        {
            _resetRequested = true;
        }


        /// <summary>
        ///     Safely reconnect to the slave device
        /// </summary>
        private void Reconnect()
        {
            // Try to close the port before we re instantiate. If this
            // explodes there are bigger issues
            _port.Disconnect();

            // Let the port cool off (close base stream, etc.)
            Thread.Sleep(100);

            Connect();
        }


        /// <summary>
        ///     Polls the slave and processes messages accordingly
        /// </summary>
        private void StartRS232Loop()
        {
            if (IsRunning)
            {
                Log.Error("Already running RS-232 Comm loop... Exiting now...");
                return;
            }


            // Polls the slave using the interval defined in config.PollRate (milliseconds)
            var speakThread = new Thread(fn =>
            {
                IsRunning = true;

                // Set toggle flag so we can kill this loop
                while (IsRunning)
                {
                    try
                    {
                        if (_resetRequested)
                        {
                            ResetAcceptor();
                        }
                        else
                        {
                            ReadAcceptorResp();
                        }

                        Thread.Sleep(Config.PollRate);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RS232 Loop port error", ex);
                        NotifyError(Errors.PortError);
                        IsRunning = false;
                    }
                }
            }) {IsBackground = true};

            speakThread.Start();
        }

        /// <summary>
        ///     The main parsing routine
        /// </summary>
        private void ReadAcceptorResp()
        {
            var data = PreviouslySentMasterMsg ?? GenerateNormalMessage();

            // Attempt to write data to slave            
            NotifySerialData(DebugBufferEntry.AsMaster(data));
            WriteWrapper(data);


            // Blocks until all 11 bytes are read or we give up
            var resp = ReadWrapper();

            var validator = new SlaveDataValidator(resp);

            if (!validator.IsValid)
            {
                NotifyProtocolViolation(validator);
            }

            // Extract only the states and events
            NotifySerialData(DebugBufferEntry.AsSlave(resp));

            // No data was read, return!!
            if (resp.Length == 0)
            {
                // Do no toggle the ack
                return;
            }

            // Check that we have the same ACK #

            if (IsBadAckNumber(resp, data))
            {
                PreviouslySentMasterMsg = data;
                return;
            }

            if (IsBadSlaveChecksumOk(resp))
            {
                PreviouslySentMasterMsg = data;
                return;
            }

            // Otherwise we're all good - toggle ack and clear last sent message

            PreviouslySentMasterMsg = null;
            Ack ^= 1;

            // At this point we have sent our message and received a valid response.
            // Parse the response and act on any state or events that are reported.


            // Translate raw bytes into friendly enums
            var slaveMessage = SlaveCodex.ToSlaveMessage(resp);
            PreviousState = SlaveCodex.GetState(slaveMessage);


            // Raise a state changed notice for clients
            NotifyStateChange(PreviousState);


            // Multiple event may be reported at once
            var currentEvents = SlaveCodex.GetEvents(slaveMessage);
            foreach (Events e in Enum.GetValues(typeof(Events)))
            {
                // If flag is set in slave message, report event
                if ((currentEvents & e) == e)
                {
                    NotifyEvent(e);
                }
            }

            // Check for cassette missing - reports every time cashbox is missing
            if (!SlaveCodex.IsCashboxPresent(slaveMessage))
            {
                CashboxPresent = false;

                NotifyError(Errors.CashboxMissing);
            }
            // Only report the cashbox attached 1 time after it is re-attached
            else if (!CashboxPresent)
            {
                CashboxPresent = true;

                SafeEvent(OnCashboxAttached);
            }


            // Mask away rest of message to see if a note is in escrow. If this is the first
            // escrow message, start the escrow timeout clock
            if (!NoteIsEscrowed && PreviousState == States.Escrowed)
            {
                _escrowStart = DateTime.MinValue;
            }

            NoteIsEscrowed = PreviousState == States.Escrowed;

            // Credit bits are 3-5 of data byte 3 
            var value = SlaveCodex.GetCredit(slaveMessage);
            if (value > 0)
            {
                Credit = (byte) value;
            }

            // Per the spec, credit message is issued by master after stack event is 
            // sent by the slave. If the previous event was stacked or returned, we
            // must clear the escrow command to completely release the note from
            // escrow state.
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (PreviousEvents)
            {
                case Events.Stacked:
                    NotifyCredit(Credit);
                    // C# does not allow fallthrough so we will goto :)
                    goto case Events.Returned;

                case Events.Returned:
                    // Clear our the pending escrow command once we've stacked or returned the note
                    EscrowCommand = EscrowCommands.None;
                    Credit = 0;
                    break;
            }

            // Update the events aster the check for check so as to not lose a credit message
            PreviousEvents = currentEvents;
        }


        /// <summary>
        ///     Generate the next master message using our given state
        /// </summary>
        /// <returns></returns>
        private byte[] GenerateNormalMessage()
        {
            //     # basic message   0      1      2      3      4      5    6      7
            //                      start, len,  ack, bills,escrow,resv'd,end, checksum
            var data = Request.BaseMessage;

            // Toggle message number (ack #) if last message was okay and not a re-send request.
            data[2] = (byte) (0x10 | Ack);

            // Get enable mask from client configuration. On next message, the acceptor
            // will update itself and not escrow any notes that are disabled in this mask.
            data[3] = Config.EnableMask;


            if (!Config.IsEscrowMode)
            {
                // Clear escrow mode bit
                data[4] = 0x00;

                if (NoteIsEscrowed)
                {
                    data[4] |= 0x20;
                }
            }
            else
            {
                // Set escrow mode bit
                data[4] = 1 << 4;

                if (!NoteIsEscrowed)
                {
                    return Checksum(data);
                }

                // Note is in escrow, we have not yet notified the client, and we did not just stack
                // a note. Due to the polling loop, we will always send an escrow message immediately
                // after a stack because we are really reporting on state during the prior poll.                    
                if (EscrowCommand == EscrowCommands.None)
                {
                    EscrowCommand = EscrowCommands.Notify;
                }

                // Otherwise do what the host tells us to do.
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (EscrowCommand)
                {
                    case EscrowCommands.Stack:
                        // set stack bit
                        data[4] |= 0x20;
                        EscrowCommand = EscrowCommands.Acknowledged;
                        break;

                    case EscrowCommands.Reject:
                        // set reject bit
                        data[4] |= 0x40;
                        EscrowCommand = EscrowCommands.Acknowledged;
                        break;

                    case EscrowCommands.Notify:
                        // notify client we have a bill in escrow
                        NotifyEscrow(Credit);
                        EscrowCommand = EscrowCommands.Awaiting;
                        break;

                    case EscrowCommands.Awaiting:
                        // Perform timeout check and set reject flag is timeout exceeded if we are awaiting stack/return command
                        if (Config.EscrowTimeoutSeconds > 0)
                        {
                            if (_escrowStart == DateTime.MinValue)
                            {
                                _escrowStart = DateTime.Now;
                            }

                            var delta = DateTime.Now - _escrowStart;
                            if (delta.TotalSeconds > Config.EscrowTimeoutSeconds)
                            {
                                EscrowCommand = EscrowCommands.Reject;

                                _escrowStart = DateTime.MinValue;
                            }
                        }

                        break;

                    case EscrowCommands.Acknowledged:
                        EscrowCommand = EscrowCommands.None;
                        break;

                    case EscrowCommands.None:
                        // do nothing
                        break;
                }
            }


            // Set the checksum
            return Checksum(data);
        }

        /// <summary>
        ///     Write data to port and notify client of any errors they should know about.
        /// </summary>
        /// <param name="data">byte[]</param>
        private void WriteWrapper(byte[] data)
        {
            try
            {
                _port.Write(data);
            }
            catch (PortException pe)
            {
                switch (pe.ErrorType)
                {
                    case ExceptionTypes.WriteError:
                        NotifyError(Errors.WriteError);
                        break;
                    case ExceptionTypes.PortError:
                        NotifyError(Errors.PortError);
                        break;

                    default:
                        throw pe.GetBaseException();
                }
            }
        }

        /// <summary>
        ///     Read data from the port and notify client of any errors they should know about.
        /// </summary>
        /// <returns></returns>
        private byte[] ReadWrapper()
        {
            try
            {
                return _port.Read();
            }
            catch (PortException pe)
            {
                switch (pe.ErrorType)
                {
                    case ExceptionTypes.Timeout:
                        NotifyError(Errors.Timeout);
                        break;

                    default:
                        throw pe.GetBaseException();
                }

                return new byte[0];
            }
        }

        /// <summary>
        ///     Perform a reset on the acceptor. This will not generate a response but the unit
        ///     may go unresponsive for up to 3 seconds.
        /// </summary>
        private void ResetAcceptor()
        {
            var reset = Request.ResetTarget;
            // Toggle message number (ack #) if last message was okay and not a re-send request.
            reset[2] = (byte) (Ack == 0 ? 0x60 : 0x61);
            reset = Checksum(reset);
            WriteWrapper(reset);

            // Toggle ACK number
            Ack ^= 1;
            _resetRequested = false;
        }

        /// <summary>
        ///     XOR checksum of only the data portion of the message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private static byte[] Checksum(IList<byte> msg)
        {
            var tmp = new List<byte>(msg);
            var checksum = (byte) (msg[1] ^ msg[2]);
            for (var i = 3; i < msg.Count - 1; i++)
            {
                checksum ^= msg[i];
            }

            tmp.Add(checksum);
            return tmp.ToArray();
        }

        /// <summary>
        ///     Returns true if the ACK numbers for the given packets do not match
        /// </summary>
        /// <param name="resp">byte[] received</param>
        /// <param name="data">byte[] last message sent</param>
        /// <returns></returns>
        private static bool IsBadAckNumber(byte[] resp, byte[] data)
        {
            return (resp[2] & 1) != (data[2] & 1);
        }

        private static bool IsBadSlaveChecksumOk(byte[] msg)
        {
            // If the length is incorrect, the checksum doesn't matter
            if (msg.Length != 11)
            {
                return true;
            }

            // msg should be the entire message, including checksum. -2 to skip checksum
            var checksum = (byte) (msg[1] ^ msg[2]);
            for (var i = 3; i < msg.Length - 2; i++)
            {
                checksum ^= msg[i];
            }

            return checksum != msg[10];
        }
    }
}