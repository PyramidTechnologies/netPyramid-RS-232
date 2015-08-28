using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Threading;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        // ms
        private static readonly int POLL_RATE = 200;
        // seconds
        private static readonly int SLAVE_DEAD_LIMIT = 10;

        private SerialPort port = null;
        private COMPort comPort = COMPort.COM1;

        // State variables for tracking between events and states
        private volatile byte ack = 0;

        // Slaves last state
        private volatile Response lastResponse = Response.Idle;

        // Track if we have already reported the cashbox state. We always
        // raise the cashbox missing event but report cashbox attached event
        // only once.
        private volatile bool cashboxPresent = true;

        // Escrow mode allows you to manually call Stack() or Reject() on each
        // escrowed note. If false, we stack any valid note automatically.
        private volatile bool isEscrowMode = false;

        // If true, the slave is reporting that a note is in escrow
        private volatile bool isEcrowed = false;

        // Last reported credit from slave
        private volatile byte credit = 0;

        // Additional feature: async flag to tell the slave
        // to ACCEPT or REJECT the note next time the master polls
        private volatile EscrowCommands escrowCommand = EscrowCommands.None;

        // If true the underlying comm port is in the open state
        private volatile bool isConnected = false;

        // Integer poll rate between 50 and 5000 ms
        private int pollRate = POLL_RATE;

        // Track comm timeout from slave device
        private DateTime escrowTimeout = DateTime.MinValue;
        private int reconnectAttempts = 0;

        // Used to map between reported value and actual currency denomination
        private CultureInfo currentCulture;
        private CurrencyMap currentCurrencyMap;


        private readonly object mutex = new object();

        /// <summary>
        /// Creates a new ApexValidator using the current system culture and not in
        /// escrow mode
        /// </summary>
        /// <param name="comm"></param>
        public ApexValidator(string comm)
        {
            comPort = (COMPort)Enum.Parse(typeof(COMPort), comm);
            new ApexValidator(comPort, CultureInfo.CurrentCulture, false);
        }

        /// <summary>
        /// Creates a new ApexValidator using the specified system culture and not in
        /// escrow mode
        /// </summary>
        /// <param name="comm"></param>
        public ApexValidator(string comm, CultureInfo culture)
        {
            comPort = (COMPort)Enum.Parse(typeof(COMPort), comm);
            new ApexValidator(comPort, culture, false);
        }

        /// <summary>
        /// Creates a new ApexValidator using the current system culture and not in
        /// escrow mode
        /// </summary>
        /// <param name="comm"></param>
        public ApexValidator(COMPort comm)
        {
            new ApexValidator(comm, CultureInfo.CurrentCulture, false);
        }

        /// <summary>
        /// Creates a new ApexValidator using the specified system culture and not in
        /// escrow mode
        /// </summary>
        /// <param name="comm"></param>
        public ApexValidator(COMPort comm, CultureInfo culture)
        {
            new ApexValidator(comm, culture, false);
        }

        /// <summary>
        /// Creates a new ApexValidator using the specified system culture in
        /// escrow mode
        /// </summary>
        /// <param name="comm"></param>
        public ApexValidator(string comm, CultureInfo culture, bool escrowModeEnabled)
        {
            comPort = (COMPort)Enum.Parse(typeof(COMPort), comm);
            new ApexValidator(comPort, culture, false);
        }

        /// <summary>
        /// Creates a new ApexValidator using the specified system culture and in
        /// escrow mode
        /// </summary>
        /// <param name="comm"></param>
        public ApexValidator(COMPort comm, CultureInfo culture, bool escrowModeEnabled)
        {
            comPort = comm;
            currentCulture = culture;
            this.isEscrowMode = escrowModeEnabled;
        }

        /// <summary>
        /// Close the underlying comm port
        /// </summary>
        public void Close()
        {
            port.Close();
        }

        /// <summary>
        /// Connect to the device and begin speaking rs232
        /// </summary>
        public void Connect()
        {

            lock (mutex)
            {
                port = new SerialPort(comPort.ToString(), 9600, Parity.Even, 7, StopBits.One);
                port.ReadTimeout = 500;

                try
                {
                    port.Open();
                }
                catch (Exception e)
                {
                    //return;
                    if (OnError != null)
                    {
                        OnError(this, ErrorTypes.PortError);
                    }
                }

                startRS232Loop();
            }         
        }

        /// <summary>
        /// Gets or sets the poll rate in milliseconds. The polled system is designed for the master to request 
        /// information from the slave at a periodic rate. The rate can be as slow as 5 seconds or as fast as 
        /// 50 msec between each poll. The popular rate is fast since the overall system performance 
        /// (bills per minute accepted) will be slower at slower polling rates. While feeding the bill into the 
        /// acceptor, the acceptor will miss a few polls, because it is reading the bill and not servicing the 
        /// serial interface (Typical for acceptors using this protocol).               
        /// </summary>
        /// <value>Min: 50 Max: 5000</value>
        public int PollRate
        {
            get
            {
                return pollRate;
            }
            set
            {
                // Allow floor of 50 ms, celing of 5 seconds
                if (value < 50 || value > 5000)
                {
                    throw new ArgumentOutOfRangeException("Minimum value is 50ms, maximum value is 5000 ms");
                }

                pollRate = value;
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
                    

                    Thread.Sleep(pollRate);
                }
            });
            ackThread.IsBackground = true;
            ackThread.Start();
        }

        /// <summary>
        /// Safely reconnect to the slave device
        /// </summary>
        private void Reconnect()
        {

            // Try to close the port before we re instantiate. If this
            // explodes there are bigger issues
            port.Close();

            // Let the port cool off (close base stream, etc.)
            Thread.Sleep(100);

            Connect();
        }

        /// <summary>
        /// The main parsing routine
        /// </summary>
        private void speakToSlave()
        {
            //     # basic message   0      1      2      3      4      5    6      7
            //                      start, len,  ack, bills,escrow,resv'd,end, checksum
            var data = Request.BaseMessage;

            // Toggle message number (ack #) if last message was okay and not a re-send request.
            data[2] = (byte)(0x10 | this.ack);
            this.ack ^= 1;

            // If we have a valid note in escrow decide if 
            // we have to wait for the host to accept/reject
            // or if we can just stack.
            if (this.isEcrowed)
            {
                if (!this.isEscrowMode)
                {

                    // Not escrow mode, we have a non-zero credit so just stack
                    data[4] |= 0x20;


                }
                else
                {
                    // Otherwise do what the host tells us to do.
                    switch (escrowCommand)
                    {
                        case EscrowCommands.Stack:
                            // set stack bit
                            data[4] |= 0x20;
                            escrowCommand = EscrowCommands.Pending;
                            break;

                        case EscrowCommands.Reject:
                            // set reject bit
                            data[4] |= 0x40;
                            escrowCommand = EscrowCommands.Pending;
                            break;

                        case EscrowCommands.Pending:
                            // Wait indefiniately for acecpt/reject command or complete
                            break;

                        case EscrowCommands.None:
                            escrowCommand = EscrowCommands.Pending;
                            OnEscrow(this, credit);
                            break;
                    }
                }
            }

            

            // Set the checksum
            data = Checksum(data);

            // Attempt to write data to slave
            Write(data);

            // Blocks until all 11 bytes are read or we give up
            var resp = Read();


            // POSSIBLE FUNCTION EXIT!!
            // No data was read, return!!
            if (resp.Length == 0)
                return;



            // With the exception of Stacked and Returned, only we can
            // only be in one state at once
            lastResponse = (Response)resp[3];

            // Only one state will be reported at once TODO


            // Mask away rest of message to see if a note is in escrow
            isEcrowed = (resp[3] & 4) == 0x04 ? true : false;


            // Multiple event may be reported at once
            if ((resp[4] & 0x01) == 0x01)
                Cheated(this, null);

            if ((resp[4] & 0x02) == 0x02)
                Rejected(this, null);

            if ((resp[4] & 0x04) == 0x04)
                Jammed(this, null);

            if ((resp[4] & 0x08) == 0x08)
                CashboxFull(this, null);


            // Check for cassette missing
            if ((resp[4] & 0x10) != 0x10)
            {

                this.cashboxPresent = false;

                CashboxRemoved(this, null);


            }

            // Only report the cashbox attached 1 time after it is re-attached
            else if (!this.cashboxPresent)
            {

                this.cashboxPresent = true;

                CashboxAttached(this, null);

            }

            // Credit bits are 3-5 of data byte 3 
            var value = (byte)((resp[5] & 0x38) >> 3);
            if (value != 0)
            {
                credit = value;

            }


            // Per the spec, credit message is issued by master after stack event is 
            // sent by the slave.
            if ((lastResponse & Response.Stacked) == Response.Stacked)
            {
                OnCredit(this, credit);
            }
            
                       
        }
    
        /// <summary>
        /// Synchronous write function that allows for up to 3 attempts to write to port.
        /// </summary>
        /// <param name="data"></param>
        private void Write(byte[] data)
        {
            if (port != null)
            {
                try
                {
                    port.Write(data, 0, data.Length);
                    reconnectAttempts = 0;
                }
                catch (Exception e)
                {
                    isConnected = false;
                    Thread.Sleep(1000);
                    reconnectAttempts++;
                    if (reconnectAttempts < 3)
                    {
                        Reconnect();
                        Write(data);
                    }
                    else
                    {
                        reconnectAttempts = 0;
                        if (OnError != null)
                        {
                            OnError(this, ErrorTypes.WriteError);
                        }
                    }
                }
            }
            else
            {
                if (OnError != null)
                {
                    OnError(this, ErrorTypes.PortError);
                }
            }
        }


        /// <summary>
        /// Synchronous read function that allows for up to 3 attempts to read from port.
        /// </summary>
        /// <param name="data"></param>
        private byte[] Read()
        {
            // Create empty array to hold incoming data
            byte[] buffer = new byte[0];

            if (port.IsOpen)
            {
                int waitCount = 0;
                while (port.BytesToRead < 11)
                {
                    waitCount++;
                    if (waitCount >= 5)
                    {
                        OnError(this, ErrorTypes.Timeout);
                        return buffer;
                    }

                    Thread.Sleep(100);
                }

                // Resize to 11 bytes since we have data to read
                buffer = new byte[11];

                port.Read(buffer, 0, 11);
                port.DiscardInBuffer();

                // Reset the timeout clock
                escrowTimeout = DateTime.MinValue;
            }

            return buffer;
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
    }
}
