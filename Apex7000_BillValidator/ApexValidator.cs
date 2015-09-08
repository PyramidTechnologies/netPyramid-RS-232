using log4net;
using PTI.Serial;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator : IDisposable
    {
      
        private readonly object mutex = new object();
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Fields
        private StrongPort port = null;
        private RS232Config config;
        private bool resetRequested = false;
        #endregion


        #region Internal State
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
        private byte[] previouslySentMasterMsg { get; set; }

        // Time at which escrow starts
        private DateTime escrowStart = DateTime.MinValue;

        private void notifySerialData(DebugBufferEntry entry)
        {
            OnSerialDataHandler handler = OnSerialData;
            if (OnSerialData != null)
            {
                handler(this, new DebugEntryArgs(entry));
            }
        }
        #endregion

        /// <summary>
        /// Creates a new ApexValidator using the specified configuration
        /// </summary>
        /// <param name="comm"></param>
        public ApexValidator(RS232Config config)
        {
            this.config = config;
        }


        /// <summary>
        /// Returns a list of all available ports
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAvailablePorts()
        {
            return StrongPort.GetAvailablePorts();
        }


        /// <summary>
        /// Stop talking to the slave and release the underlying commm port.
        /// </summary>
        public void Close()
        {
            // This will kill the comms loop
            config.IsRunning = false;

            if(port != null)
                port.Disconnect();
        }



        /// <summary>
        /// Connect to the device and begin speaking rs232
        /// </summary>
        public void Connect()
        {


            // Lock so we only have one connection attempt at a time. This protects
            // from client code behaving badly.
            lock (mutex)
            {

                try
                {
                    port = new StrongPort(config.CommPortName);
                }
                catch (IOException)
                {
                    NotifyError(Errors.FailedToOpenPort);
                    return;
                }

                port.ReadTimeout = 500;

                DebugBufferEntry.SetEpoch();

                try
                {
                    port.Connect();


                    // Only start if we connect without error
                    startRS232Loop();

                }
                catch (Exception e)
                {

                    log.ErrorFormat("Exception Connecting to acceptor: {0}", e.Message, e);


                    if (OnError != null)
                    {

                        NotifyError(Errors.PortError);

                    }

                }


            }         
        }

        /// <summary>
        /// Sets flag to reset the acceptor on the next message sent
        /// </summary>
        public void RequestReset()
        {
            resetRequested = true;
        }



        /// <summary>
        /// Safely reconnect to the slave device
        /// </summary>
        private void Reconnect()
        {

            // Try to close the port before we re instantiate. If this
            // explodes there are bigger issues
            port.Disconnect();

            // Let the port cool off (close base stream, etc.)
            Thread.Sleep(100);

            Connect();
        }



        /// <summary>
        /// Polls the slave and processes messages accordingly
        /// </summary>
        private void startRS232Loop()
        {

            if (config.IsRunning)
            {
                log.Error("Already running RS-232 Comm loop... Exiting now...");
                return;
            }


            // Polls the slave using the interval defined in config.PollRate (milliseconds)
            Thread speakThread = new Thread((fn) =>
            {

                config.IsRunning = true;

                // Set toggle flag so we can kill this loop
                while (config.IsRunning)
                {

                    if (resetRequested)
                        DoResetAcceptor();
                    else
                        speakToSlave();                  

                    Thread.Sleep(config.PollRate);
                }

            });

            speakThread.IsBackground = true;
            speakThread.Start();
        }


        #region Implementation
        /// <summary>
        /// The main parsing routine
        /// </summary>
        private void speakToSlave()
        {
            byte[] data;
            if (previouslySentMasterMsg == null)
            {
                data = GenerateNormalMessage();
            }
            else
            {
                data = previouslySentMasterMsg;

            }
           
            // Attempt to write data to slave            
            notifySerialData(DebugBufferEntry.AsMaster(data));
            WriteWrapper(data);


            // Blocks until all 11 bytes are read or we give up
            var resp = ReadWrapper();


            // Extract only the states and events
            notifySerialData(DebugBufferEntry.AsSlave(resp));
            
            // No data was read, return!!
            if (resp.Length == 0)
            {
                // Do no toggle the ack
                return;
            }

            // Check that we have the same ACK #
            else if (IsBadAckNumber(resp, data))
            {
                previouslySentMasterMsg = data;
                return;
            }

            // TODO check response checksum



            // Otherwise we're all good - toggle ack and clear last sent message
            else
            {
                previouslySentMasterMsg = null;
                Ack ^= 1;
            }
            
            // At this point we have sent our message and received a valid response.
            // Parse the response and act on any state or events that are reported.


            // Translate raw bytes into friendly enums
            var slaveMessage = SlaveCodex.ToSlaveMessage(resp);
            config.PreviousState = SlaveCodex.GetState(slaveMessage);


            // Raise a state changed notice for clients
            NotifyStateChange(config.PreviousState);
           

            // Multiple event may be reported at once
            var currentEvents = SlaveCodex.GetEvents(slaveMessage);
            foreach (Events e in Enum.GetValues(typeof(Events)))
            {
                // If flag is set in slave message, report event
                if((currentEvents & e) == e)
                    NotifyEvent(e);
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
            if(!NoteIsEscrowed && config.PreviousState == States.Escrowed)
            {
                escrowStart = DateTime.MinValue;
            }
            NoteIsEscrowed = (config.PreviousState == States.Escrowed);

            // Credit bits are 3-5 of data byte 3 
            var value = SlaveCodex.GetCredit(slaveMessage);
            if (value > 0)
            {
                Credit = (byte)value;

            }

            // Per the spec, credit message is issued by master after stack event is 
            // sent by the slave. If the previous event was stacked or returned, we
            // must clear the escrow command to completely release the note from
            // escrow state.
            switch(config.PreviousEvents)
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
            config.PreviousEvents = currentEvents;
                       
        }
    
        #region GenerateMsg Write Read Checksum
        /// <summary>
        /// Generate the next master message using our given state
        /// </summary>
        /// <returns></returns>
        private byte[] GenerateNormalMessage()
        {
            //     # basic message   0      1      2      3      4      5    6      7
            //                      start, len,  ack, bills,escrow,resv'd,end, checksum
            var data = Request.BaseMessage;

            // Toggle message number (ack #) if last message was okay and not a re-send request.
            data[2] = (byte)(0x10 | Ack);



            if(!config.IsEscrowMode)
            {

                // Not escrow mode, all notes are enabled by default
                data[3] = 0x7F;

                // Clear escrow mode bit
                data[4] = 0x00;

                if (NoteIsEscrowed)
                    data[4] |= 0x20;

            } 
            else
            {

                // Get enable mask from client configuration. On next message, the acceptor
                // will update itself and not escrow any notes that are disabled in this mask.
                data[3] = config.EnableMask;

                // Set escrow mode bit
                data[4] = 1 << 4;

                if(NoteIsEscrowed)
                {
                    // Perform timeout check and set reject flag is timeout exceeded
                    if(config.EscrowTimeoutSeconds > 0)
                    {

                        if(escrowStart == DateTime.MinValue)
                        {
                            escrowStart = DateTime.Now;
                        }

                        var delta = DateTime.Now - escrowStart;
                        if (delta.TotalSeconds > config.EscrowTimeoutSeconds)
                        {
                            EscrowCommand = EscrowCommands.Reject;
                            
                            escrowStart = DateTime.MinValue;
                        }
                    }


                    // Otherwise do what the host tells us to do.
                    switch (EscrowCommand)
                    {
                        case EscrowCommands.Stack:
                            // set stack bit
                            data[4] |= 0x20;
                            EscrowCommand = EscrowCommands.None;
                            break;

                        case EscrowCommands.Reject:
                            // set reject bit
                            data[4] |= 0x40;
                            EscrowCommand = EscrowCommands.None;
                            break;

                        case EscrowCommands.None:
                            NotifyEscrow(Credit);
                            break;
                    }
                }

            }


            // Set the checksum
            return Checksum(data);
        }

        /// <summary>
        /// Write data to port and notify client of any errors they should know about.
        /// </summary>
        /// <param name="data">byte[]</param>
        private void WriteWrapper(byte[] data)
        {
            try
            {
                port.Write(data);

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
        /// Read data from the port and notify client of any errors they should know about.
        /// </summary>
        /// <returns></returns>
        private byte[] ReadWrapper()
        {
            try
            {
                return port.Read();

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
        /// Perform a reset on the acceptor. This will not generate a response but the unit
        /// may go unresponsive for up to 3 seconds.
        /// </summary>
        private void DoResetAcceptor()
        {
            byte[] reset = Request.ResetTarget;
            // Toggle message number (ack #) if last message was okay and not a re-send request.
            reset[2] = (byte)(0x10 | Ack);
            Checksum(reset);
            WriteWrapper(reset);

            // Toggle ACK number
            Ack ^= 1;
            resetRequested = false;
        }

        /// <summary>
        /// XOR checksum of only the data portion of the message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private byte[] Checksum(byte[] msg)
        {
            List<byte> tmp = new List<byte>(msg);
            byte checksum = (byte)(msg[1] ^ msg[2]);
            for (int i = 3; i < msg.Length - 1; i++)
            {
                checksum ^= msg[i];
            }

            tmp.Add(checksum);
            return tmp.ToArray();
        }

        /// <summary>
        /// Returns true if the ACK numbers for the given packets do not match
        /// </summary>
        /// <param name="resp">byte[] received</param>
        /// <param name="data">byte[] last message sent</param>
        /// <returns></returns>
        private static bool IsBadAckNumber(byte[] resp, byte[] data)
        {
            return (resp[2] & 1) != (data[2] & 1);
        }
        #endregion
        #endregion

        #region IDisposable
        /// <summary>
        /// Releases comm port and related managed resources.
        /// </summary>
        public void Dispose()
        {
            if (port != null)
                port.Dispose();
        }
        #endregion
    }
}
