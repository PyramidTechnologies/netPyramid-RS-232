using System;
using System.IO.Ports;
using System.Threading;

namespace PTI.Serial
{

    class MonoPort : ICommPort
    {        
        // My prefered encoding when debugging byte buffers
        private static readonly System.Text.Encoding W1252 = System.Text.Encoding.GetEncoding("Windows-1252");

        private SerialPort port;
        // Lock on reading so we don't disconnect while waiting for data.
        private object readLock = new object();

        public MonoPort(string portName)
        {
            port = new SerialPort(portName);
            port.BaudRate = 9600;
            port.Parity = System.IO.Ports.Parity.Even;
            port.DataBits = 7;
            port.StopBits = StopBits.One;
            port.Handshake = Handshake.None;
            port.ReadTimeout = 500;
            port.WriteTimeout = 500;
            port.WriteBufferSize = 1000;
            port.ReadBufferSize = 1000;
            port.Encoding = W1252;
            port.DtrEnable = true;
            port.RtsEnable = true;
        }

        public bool IsOpen { get { return port.IsOpen; } }

        public string Name
        {
            get { return port.PortName; }
            set { port.PortName = value; }
        }

        public int ReadTimeout
        {
            get { return port.ReadTimeout; }

            set { port.ReadTimeout = value; }
        }

        public bool Connect()
        {
            port.Open();
            return true;
        }

        public bool Disconnect()
        {
            port.Close();
            return true;
        }

        /// <summary>
        /// Writes the byte[] data to this port. If the port is not open, misconfigured,
        /// or if there is a physical connection issue, exceptions may arise.
        /// </summary>
        /// <param name="data">byte[]</param>
        public void Write(byte[] data)
        {
            int reconnectAttempts = 0;
            if (port != null)
            {
                try
                {
                    port.Write(data, 0, data.Length);
                    reconnectAttempts = 0;
                }
                catch (Exception)
                {
                    // Incremental backoff
                    Thread.Sleep(500 * reconnectAttempts++);
                    if (reconnectAttempts < 3)
                    {
                        Disconnect();
                        new StrongPort(port.PortName);
                        Write(data);
                    }
                    else
                    {
                        throw new PortException(ExceptionTypes.WriteError);
                    }
                }
            }
            else
            {
                throw new PortException(ExceptionTypes.PortError);
            }
        }

        /// <summary>
        /// Reads all available data from this port. If no data is received withint
        /// ReadTimeout milliseconds, a timeout exception will be raised.
        /// </summary>
        /// <returns>byte[]</returns>
        public byte[] Read()
        {
            // Create empty array to hold incoming data
            byte[] buffer = new byte[0];

            lock (readLock)
            {
                if (IsOpen)
                {
                    int waitCount = 0;
                    while (port.BytesToRead < 11)
                    {
                        waitCount++;
                        if (waitCount >= 5)
                        {
                            throw new PortException(ExceptionTypes.Timeout);
                        }

                        Thread.Sleep(100);
                    }

                    // Resize to 11 bytes since we have data to read
                    buffer = new byte[11];

                    port.Read(buffer, 0, 11);
                    port.DiscardInBuffer();

                }
            }

            return buffer;
        }

        #region Disposal
        /// <summary>
        /// Releases comm port and related managed resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Releases comm port and related managed resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }
}
