using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Threading;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        private SerialPort port = null;
        private byte lastResponse = Response.Idle;
        private COMPort comPort = COMPort.COM1;
        private readonly object mutex = new object();
        public bool isConnected = false;
        private int reconnectAttempts = 0;
        private DateTime escrowTimeout = DateTime.MinValue;
        private CultureInfo currentCulture;
        private CurrencyMap currentCurrencyMap;
        

        public ApexValidator(string comm)
        {
            comPort = (COMPort)Enum.Parse(typeof(COMPort), comm);
        }

        public ApexValidator(COMPort comm)
        {
            comPort = comm;
        }

        public ApexValidator(COMPort comm, string culture)
        {
            comPort = comm;
            currentCulture = new CultureInfo(culture);
        }

        public ApexValidator(COMPort comm, CultureInfo culture)
        {
            comPort = comm;
            currentCulture = culture;
        }

        public ApexValidator(string comm, CultureInfo culture)
        {
            comPort = (COMPort)Enum.Parse(typeof(COMPort), comm);
            currentCulture = culture;
        }


        public void Close()
        {
            port.Close();
        }

        public void Connect()
        {
            //if (port != null)
            //    port.DataReceived -= port_DataReceived;

            port = new SerialPort(comPort.ToString(), 9600, Parity.Even, 7, StopBits.One);
            port.ReadTimeout = 500;
            //port.DataReceived += port_DataReceived;

            try
            {
                port.Open();
            }
            catch(Exception e)
            {
                /* Handle this better? */
                if (OnError != null)
                {
                    OnError(this, ErrorTypes.PortError);
                }
                else
                {
                    throw e;
                }
            }

            byte[] wakeup = Request.BaseMessage;
            
            lock(mutex)
            {
                Write(wakeup);
                Read();
            }

            Thread ackThread = new Thread((fn) =>
            {
                while(true)
                {
                    lock (mutex)
                    {
                        Write(Request.BaseMessage);
                        Read();
                    }

                    Thread.Sleep(200);
                }
            });
            ackThread.IsBackground = true;
            ackThread.Start();

        }

        private void Reconnect()
        {
            lock(mutex)
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

                byte[] wakeup = Request.BaseMessage;

                lock (mutex)
                {
                    Write(wakeup);
                    Read();
                }
            }
        }

        private void Write(byte[] data)
        {
            data = Checksum(data);

            if (port != null)
            {
                try
                {
                    port.Write(data, 0, data.Length);
                    reconnectAttempts = 0;
                }
                catch(Exception e)
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

        private byte[] Checksum(byte[] msg)
        {
            List<byte> tmp = new List<byte>(msg);
            byte checksum = (byte)(msg[1] ^ msg[2]);
            for(int i = 3; i < msg.Length - 1; i++)
            {
                checksum ^= msg[i];
            }

            tmp.Add(checksum);
            return tmp.ToArray();
        }

        private void Read()
        {
            if (port.IsOpen)
            {
                int waitCount = 0;
                while (port.BytesToRead < 11)
                {
                    waitCount++;
                    if (waitCount >= 5)
                    {
                        /* Don't report error if validator is in accepting state as this can take some time */
                        //if (OnError != null && lastResponse != Response.Accepting)
                        //{
                            OnError(this, ErrorTypes.Timeout);
                        //}

                        return;
                    }

                    Thread.Sleep(100);
                }

                byte[] buffer = new byte[11];

                port.Read(buffer, 0, 11);

                byte t3 = buffer[3];
                byte t4 = buffer[4];

                if ((t3 & 1) == Response.Idle)
                {
                    //Only if the box has been recently removed 
                    if ((t4 & 0x10) == Response.CassetteRemoved)
                    {
                        if (lastResponse != Response.CassetteRemoved)
                        {
                            lastResponse = Response.CassetteRemoved;
                            if (CashboxRemoved != null)
                            {
                                CashboxRemoved(this, null);
                            }
                        }
                    }
                    else
                    {

                        //Normal idle response
                        if (lastResponse == Response.CassetteRemoved)
                        {
                            if (CashboxAttached != null)
                            {
                                CashboxAttached(this, null);
                            }
                        }

                        if (!isConnected)
                        {
                            isConnected = true;
                            if (PowerUp != null)
                            {
                                PowerUp(this, null);
                            }
                        }

                        lastResponse = Response.Idle;
                    }

                    Write(Request.Ack);
                }
                if ((t3 & 2) == Response.Accepting)
                {
                    if (lastResponse != Response.Accepting)
                    {
                        lastResponse = Response.Accepting;
                    }
                }
                if ((t3 & 4) == Response.Escrow)
                {

                    if (lastResponse != Response.Escrow)
                    {
                        if (escrowTimeout == DateTime.MinValue)
                        {
                            escrowTimeout = DateTime.Now;
                        }
                        else
                        {
                            TimeSpan ts = DateTime.Now - escrowTimeout;
                            if (ts.TotalSeconds >= 10)
                            {
                                //Let's reconnect and make sure everything is still good
                                Reconnect();
                                Reject();
                            }
                        }

                        lastResponse = Response.Escrow;

                        byte bill = (byte)((buffer[5] & 0x38) >> 3);

                        if (OnEscrow != null)
                            OnEscrow(this, BillParser.getDenomFromByte(bill, currentCulture));
                    }
                }
                if ((t3 & 8) == Response.Stacking)
                {
                    if (lastResponse != Response.Stacking)
                    {
                        lastResponse = Response.Stacking;

                        if (BillStacked != null)
                        {
                            BillStacked(this, null);
                        }
                    }
                }
                if ((t3 & 0x20) == Response.Returned)
                {
                    if (lastResponse != Response.Returned)
                    {
                        //This screws with the box when it is removed
                        //lastResponse = Response.Returned;
                        Write(Request.Ack);
                    }
                }
            }
        }
    }
}
