using PTI.Serial;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
      
        private readonly object mutex = new object();
        private static readonly slf4net.ILogger log = slf4net.LoggerFactory.GetLogger(typeof(StrongPort));

        #region Fields
        private StrongPort port = null;
        private RS232Config config;
        private bool resetRequested = false;
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



        public void Close()
        {
            // This will kill the comms loop
            config.IsRunning = false;

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


                port = new StrongPort(config.CommPortName);
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

                    if (OnError != null)
                    {

                        log.Error(e.Message);

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

        /// <summary>
        /// The main parsing routine
        /// </summary>
        private void speakToSlave()
        {
            byte[] data;
            if (config.previouslySentMasterMsg == null)
            {
                data = GenerateNormalMessage();
            }
            else
            {
                data = config.previouslySentMasterMsg;

            }
           
            // Attempt to write data to slave            
            config.notifySerialData(DebugBufferEntry.AsMaster(data));
            WriteWrapper(data);


            // Blocks until all 11 bytes are read or we give up
            var resp = ReadWrapper();


            // Extract only the states and events
            config.notifySerialData(DebugBufferEntry.AsSlave(resp));
            
            // POSSIBLE FUNCTION EXIT!!
            // No data was read, return!!
            if (resp.Length == 0)
            {
                // Do no toggle the ack
                return;
            }

            // Check that we have the same ACK #
            else if (IsBadAckNumber(resp, data))
            {
                config.previouslySentMasterMsg = data;
                return;
            }

            // TODO check response checksum



            // Otherwise we're all good - toggle ack and clear last sent message
            else
            {
                config.previouslySentMasterMsg = null;
                config.Ack ^= 1;
            }
            
            // At this point we have sent our message and received a valid response.
            // Parse the response and act on any state or events that are reported.


            // Translate raw bytes into friendly enums
            var slaveMessage = SlaveCodex.ToSlaveMessage(resp);
            config.PreviousResponse = SlaveCodex.GetState(slaveMessage);


            // Raise a state changed notice for clients
            OnStateChanged(this, config.PreviousResponse);
           

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

                config.CashboxPresent = false;

                NotifyError(Errors.CashboxMissing);

            }
            // Only report the cashbox attached 1 time after it is re-attached
            else if (!config.CashboxPresent)
            {

                config.CashboxPresent = true;

                SafeEvent(OnCashboxAttached);

            }


            // Mask away rest of message to see if a note is in escrow
            config.NoteIsEscrowed = (config.PreviousResponse == States.Escrowed);

            // Credit bits are 3-5 of data byte 3 
            var value = SlaveCodex.GetCredit(slaveMessage);
            if (value > 0)
            {
                config.Credit = (byte)value;

            }

            // Per the spec, credit message is issued by master after stack event is 
            // sent by the slave. If the previous event was stacked or returned, we
            // must clear the escrow command to completely release the note from
            // escrow state.
            switch(config.PreviousEvents)
            {
                case Events.Stacked:
                    NotifyCredit(config.Credit);
                    // C# does not allow fallthrough so we will goto :)
                    goto case Events.Returned;
    
                case Events.Returned:                                
                    // Clear our the pending escrow command once we've stacked or returned the note
                    config.EscrowCommand = EscrowCommands.None;
                    config.Credit = 0;
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
            data[2] = (byte)(0x10 | config.Ack);

            // Enable all notes
            // TODO create a mask for this
            data[3] = 0x7F;

            if(!config.IsEscrowMode)
            {
                // Clear escrow mode bit
                data[4] = 0x00;

                if (config.NoteIsEscrowed)
                    data[4] |= 0x20;

            } 
            else
            {
                // Set escrow mode bit
                data[4] = 1 << 4;

                if(config.NoteIsEscrowed)
                {

                    if(config.EscrowTimeoutSeconds > 0)
                    {

                        if(config.escrowStart == DateTime.MinValue)
                        {
                            config.escrowStart = DateTime.Now;
                        }

                        var delta = DateTime.Now - config.escrowStart;
                        if (delta.TotalSeconds > config.EscrowTimeoutSeconds)
                        {
                            config.EscrowCommand = EscrowCommands.Reject;
                            
                            config.escrowStart = DateTime.MinValue;
                        }
                    }


                    // Otherwise do what the host tells us to do.
                    switch (config.EscrowCommand)
                    {
                        case EscrowCommands.Stack:
                            // set stack bit
                            data[4] |= 0x20;
                            config.EscrowCommand = EscrowCommands.None;
                            break;

                        case EscrowCommands.Reject:
                            // set reject bit
                            data[4] |= 0x40;
                            config.EscrowCommand = EscrowCommands.None;
                            break;

                        case EscrowCommands.None:
                            NotifyEscrow(config.Credit);
                            config.escrowStart = DateTime.MinValue;
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
                        OnError(this, Errors.WriteError);
                        break;
                    case ExceptionTypes.PortError:
                        OnError(this, Errors.PortError);
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
                        OnError(this, Errors.Timeout);
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
            reset[2] = (byte)(0x10 | config.Ack);
            Checksum(reset);
            WriteWrapper(reset);

            // Toggle ACK number
            config.Ack ^= 1;
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
    }
}
