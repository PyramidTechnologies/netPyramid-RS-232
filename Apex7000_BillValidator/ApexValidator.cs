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

        #region Fields
        private StrongPort port = null;
        private RS232Config config;
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
        /// Close the underlying comm port
        /// </summary>
        public void Close()
        {
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

                try
                {
                    port.Connect();
                }
                catch (Exception e)
                {
                    if (OnError != null)
                    {
                        NotifyError(ErrorTypes.PortError);
                    }
                }

                startRS232Loop();
            }         
        }

        /// <summary>
        /// Polls the slave and processes messages accordingly
        /// </summary>
        private void startRS232Loop()
        {

            Thread ackThread = new Thread((fn) =>
            {
                while (true)
                {

                    speakToSlave();

                    //TimeSpan ts = DateTime.Now - escrowTimeout;
                    //if (ts.TotalSeconds >= SLAVE_DEAD_LIMIT)
                    //{

                    //    //Let's reconnect and make sure everything is still good
                    //    Reconnect();
                    //    Reject();

                    //}
                    

                    Thread.Sleep(config.PollRate);
                }
            });
            ackThread.IsBackground = true;
            ackThread.Start();
        }

        private void WriteWrapper(byte[] data)
        {
            try
            {
                port.Write(data);
            } catch(PortException pe)
            {
                switch(pe.ErrorType)
                {
                    case PortErrors.WriteError:
                        OnError(this, ErrorTypes.WriteError);
                        break;
                    case PortErrors.PortError:
                        OnError(this, ErrorTypes.PortError);
                        break;
                }
            }
        }

        private byte[] ReadWrapper()
        {
            try
            {
                return port.Read();

            } catch(PortException pe)
            {
                switch (pe.ErrorType)
                {
                    case PortErrors.WriteError:
                        OnError(this, ErrorTypes.WriteError);
                        break;
                    case PortErrors.PortError:
                        OnError(this, ErrorTypes.PortError);
                        break;
                }

                return new byte[0];
            }

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
            config.pushDebugEntry(DebugBufferEntry.DebugBufferEntryAsMaster(data, Thread.CurrentThread.ManagedThreadId));
            port.Write(data);

            // Blocks until all 11 bytes are read or we give up
            var resp = port.Read();
            config.pushDebugEntry(DebugBufferEntry.DebugBufferEntryAsSlave(resp, Thread.CurrentThread.ManagedThreadId));
            
            // POSSIBLE FUNCTION EXIT!!
            // No data was read, return!!
            if (resp.Length == 0)
            {
                // Do no toggle the ack
                return;
            }

            // Check that we have the same ACK #
            else if((resp[2] & 1) != (data[2] & 1))
            {
                config.previouslySentMasterMsg = data;
                return;
            }

            // Otherwise we're all good
            else
            {
                config.previouslySentMasterMsg = null;
                config.Ack ^= 1;
            }



            // With the exception of Stacked and Returned, only we can
            // only be in one state at once
            config.PreviousResponse = (Response)resp[3];

            // Only one state will be reported at once 
            // TODO


            // Mask away rest of message to see if a note is in escrow
            config.IsEscrowed = (resp[3] & 4) == 0x04 ? true : false;


            // Multiple event may be reported at once
            if ((resp[4] & 0x01) == 0x01)
                NotifyError(ErrorTypes.BillFish);

            if ((resp[4] & 0x02) == 0x02)
                NotifyError(ErrorTypes.BillReject);

            if ((resp[4] & 0x04) == 0x04)
                NotifyError(ErrorTypes.BillJam);

            if ((resp[4] & 0x08) == 0x08)
                NotifyError(ErrorTypes.CashboxFull);


            // Check for cassette missing
            if ((resp[4] & 0x10) != 0x10)
            {

                config.CashboxPresent = false;

                NotifyError(ErrorTypes.CashboxMissing);

            }

            // Only report the cashbox attached 1 time after it is re-attached
            else if (!config.CashboxPresent)
            {

                config.CashboxPresent = true;

                HandleEvent(OnCashboxAttached);

            }

            // Credit bits are 3-5 of data byte 3 
            var value = (byte)((resp[5] & 0x38) >> 3);
            if (value != 0)
            {
                config.Credit = value;

            }


            // Per the spec, credit message is issued by master after stack event is 
            // sent by the slave.
            if ((config.PreviousResponse & Response.Stacked) == Response.Stacked)
            {
                config.EscrowCommand = EscrowCommands.None;

                NotifyCredit(config.Credit);
            }            
                       
        }
    
        #region GenerateMsg Read Write Checksum
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

            // If we have a valid note in escrow decide if 
            // we have to wait for the host to accept/reject
            // or if we can just stack.
            if (config.IsEscrowed)
            {
                if (!config.IsEscrowMode)
                {

                    // Not escrow mode, we have a non-zero credit so just stack
                    data[4] |= 0x20;


                }
                else
                {
                    // Otherwise do what the host tells us to do.
                    switch (config.EscrowCommand)
                    {
                        case EscrowCommands.Stack:
                            // set stack bit
                            data[4] |= 0x20;
                            config.EscrowCommand = EscrowCommands.Pending;
                            break;

                        case EscrowCommands.Reject:
                            // set reject bit
                            data[4] |= 0x40;
                            config.EscrowCommand = EscrowCommands.Pending;
                            break;

                        case EscrowCommands.Pending:
                            // Wait indefiniately for acecpt/reject command or complete
                            break;

                        case EscrowCommands.None:
                            config.EscrowCommand = EscrowCommands.Pending;
                            NotifyEscrow(config.Credit);
                            break;
                    }
                }
            }

            // Set the checksum
            return Checksum(data);
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
        #endregion
    }
}
