namespace PTI.Serial
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using log4net;

    // Wrapper around SerialPort
    // \internal
    internal class StrongPort : ICommPort
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Lock on reading so we don't disconnect while waiting for data.
        private static readonly object ReadLock = new object();

        // My prefered encoding when debugging byte buffers
        private readonly Encoding _w1252 = Encoding.GetEncoding("Windows-1252");

        /// <summary>
        ///     Creates a new strong port by correctly configuring the DCB blocks used to configured
        ///     the comm port in the Win32 API. As such, this call requires unrestricted access to the system
        ///     e.g. run as admin. If you do not run this application as admin, this call will fail with a
        ///     security excetion
        /// </summary>
        /// <param name="portName">OS name of port to open. e.g. COM4</param>
        /// <exception cref="System.Security.SecurityException">Thrown if executing user does not have unrestricted access</exception>
        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        internal StrongPort(string portName)
        {
            // http://zachsaw.blogspot.com/2010/07/net-serialport-woes.html
            SerialPortFixer.Execute(portName);

            var port = new SerialPort
            {
                BaudRate = 9600,
                Parity = Parity.Even,
                DataBits = 7,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500,
                WriteBufferSize = 1000,
                ReadBufferSize = 1000,
                Encoding = _w1252,
                DtrEnable = true,
                RtsEnable = true,
                DiscardNull = false,
                PortName = portName
            };
            port.Open();

            try
            {
                _internalSerialStream = port.BaseStream;
                _serialPort = port;
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
            }
            catch (Exception ex)
            {
                var internalStream = _internalSerialStream;

                if (internalStream == null)
                {
                    var field = typeof(SerialPort).GetField(
                        "internalSerialStream",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    // This will happen if the SerialPort class is changed
                    // in future versions of the .NET Framework
                    if (field == null)
                    {
                        Log.WarnFormat(
                            "An exception occured while creating the serial port adaptor, "
                            + "the internal stream reference was not acquired and we were unable "
                            + "to get it using reflection. The serial port may not be accessible "
                            + "any further until the serial port object finalizer has been run: {0}",
                            ex);

                        throw;
                    }

                    internalStream = (Stream) field.GetValue(port);
                }

                Log.DebugFormat(
                    "An error occurred while constructing the serial port adaptor: {0}", ex);

                SafeDisconnect(port, internalStream);
                throw;
            }
        }

        #region Fields

        // Serial port objects
        private readonly SerialPort _serialPort;
        private readonly Stream _internalSerialStream;

        #endregion

        #region Disposal

        /// <summary>
        ///     Releases comm port and related managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Releases comm port and related managed resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            SafeDisconnect(_serialPort, _internalSerialStream);

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region Static

        /// <summary>
        ///     Safely closes a serial port and its internal stream even if
        ///     a USB serial interface was physically removed from the system
        ///     in a reliable manner.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="internalSerialStream"></param>
        /// <remarks>
        ///     The <see cref="SerialPort" /> class has 3 different problems in disposal
        ///     in case of a USB serial device that is physically removed:
        ///     1. The eventLoopRunner is asked to stop and <see cref="SerialPort.IsOpen" />
        ///     returns false. Upon disposal this property is checked and closing
        ///     the internal serial stream is skipped, thus keeping the original
        ///     handle open indefinitely (until the finalizer runs which leads to the next problem)
        ///     The solution for this one is to manually close the internal serial stream.
        ///     We can get its reference by <see cref="SerialPort.BaseStream" />
        ///     before the exception has happened or by reflection and getting the
        ///     "internalSerialStream" field.
        ///     2. Closing the internal serial stream throws an exception and closes
        ///     the internal handle without waiting for its eventLoopRunner thread to finish,
        ///     causing an uncatchable ObjectDisposedException from it later on when the finalizer
        ///     runs (which oddly avoids throwing the exception but still fails to wait for
        ///     the eventLoopRunner).
        ///     The solution is to manually ask the event loop runner thread to shutdown
        ///     (via reflection) and waiting for it before closing the internal serial stream.
        ///     3. Since Dispose throws exceptions, the finalizer is not suppressed.
        ///     The solution is to suppress their finalizers at the beginning.
        /// </remarks>
        private static void SafeDisconnect(SerialPort port, Stream internalSerialStream)
        {
            GC.SuppressFinalize(port);
            GC.SuppressFinalize(internalSerialStream);

            ShutdownEventLoopHandler(internalSerialStream);

            try
            {
                Log.Debug("Disposing internal serial stream");
                internalSerialStream.Close();
            }
            catch (Exception ex)
            {
                Log.DebugFormat(
                    "Exception in serial stream shutdown of port {0}: {1}", port.PortName, ex);
            }

            try
            {
                Log.Debug("Disposing serial port");
                port.Close();
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Exception in port {0} shutdown: {1}", port.PortName, ex);
            }
        }

        private static void ShutdownEventLoopHandler(Stream internalSerialStream)
        {
            try
            {
                Log.Debug("Working around .NET SerialPort class Dispose bug");

                var eventRunnerField = internalSerialStream.GetType()
                    .GetField("eventRunner", BindingFlags.NonPublic | BindingFlags.Instance);

                if (eventRunnerField == null)
                {
                    Log.Warn(
                        "Unable to find EventLoopRunner field. ");
                }
                else
                {
                    var eventRunner = eventRunnerField.GetValue(internalSerialStream);
                    var eventRunnerType = eventRunner.GetType();

                    var endEventLoopFieldInfo = eventRunnerType.GetField(
                        "endEventLoop", BindingFlags.Instance | BindingFlags.NonPublic);

                    var eventLoopEndedSignalFieldInfo = eventRunnerType.GetField(
                        "eventLoopEndedSignal", BindingFlags.Instance | BindingFlags.NonPublic);

                    var waitCommEventWaitHandleFieldInfo = eventRunnerType.GetField(
                        "waitCommEventWaitHandle", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (endEventLoopFieldInfo == null
                        || eventLoopEndedSignalFieldInfo == null
                        || waitCommEventWaitHandleFieldInfo == null)
                    {
                        Log.Warn(
                            "Unable to find the EventLoopRunner internal wait handle or loop signal fields.");
                    }
                    else
                    {
                        Log.Debug("Waiting for the SerialPort internal EventLoopRunner thread to finish...");

                        var eventLoopEndedWaitHandle =
                            (WaitHandle) eventLoopEndedSignalFieldInfo.GetValue(eventRunner);
                        var waitCommEventWaitHandle =
                            (ManualResetEvent) waitCommEventWaitHandleFieldInfo.GetValue(eventRunner);

                        endEventLoopFieldInfo.SetValue(eventRunner, true);

                        // Sometimes the event loop handler resets the wait handle
                        // before exiting the loop and hangs (in case of USB disconnect)
                        // In case it takes too long, brute-force it out of its wait by
                        // setting the handle again.
                        do
                        {
                            waitCommEventWaitHandle.Set();
                        } while (!eventLoopEndedWaitHandle.WaitOne(2000));

                        Log.Debug("Wait completed. Now it is safe to continue disposal.");
                    }
                }
            }
            catch (Exception)
            {
                Log.Warn("SerialPort workaround failure.");
            }
        }

        /// <summary>
        ///     Return a list of all available serial ports that the OS can connect upon
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAvailablePorts()
        {
            return SerialPort.GetPortNames().ToList();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Returns true if this port is open
        /// </summary>
        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;

        /// <summary>
        ///     Returns the OS name for the serial port
        /// </summary>
        public string Name
        {
            get => _serialPort.PortName;
            set => _serialPort.PortName = value;
        }

        /// <summary>
        ///     Set the read timeout for the underlying serial port. Any read executed on this port
        ///     that does not receive data before this timeout will cause a SerialPortTimeout exception.
        /// </summary>
        public int ReadTimeout
        {
            get => _serialPort.ReadTimeout;
            set => _serialPort.ReadTimeout = value;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     This class of port gets opened on instantiation
        /// </summary>
        /// <returns>bool</returns>
        public bool Connect()
        {
            return IsOpen;
        }

        /// <summary>
        ///     Attempts to safely close the underling serial port and base stream.
        ///     If the close operation completes without issue, bool true is returned.
        /// </summary>
        /// <returns>bool</returns>
        public bool Disconnect()
        {
            // Lock prevents closing in the middle of a read
            lock (ReadLock)
            {
                SafeDisconnect(_serialPort, _internalSerialStream);
            }

            return true;
        }

        /// <summary>
        ///     Writes the byte[] data to this port. If the port is not open, misconfigured,
        ///     or if there is a physical connection issue, exceptions may arise.
        /// </summary>
        /// <param name="data">byte[]</param>
        public void Write(byte[] data)
        {
            var reconnectAttempts = 0;
            if (_serialPort != null)
            {
                try
                {
                    _serialPort.Write(data, 0, data.Length);
                    reconnectAttempts = 0;
                }
                catch (Exception)
                {
                    // Incremental backoff
                    Thread.Sleep(500 * reconnectAttempts++);
                    if (reconnectAttempts < 3)
                    {
                        Disconnect();
                        new StrongPort(_serialPort.PortName);
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
        ///     Reads all available data from this port. If no data is received withint
        ///     ReadTimeout milliseconds, a timeout exception will be raised.
        /// </summary>
        /// <returns>byte[]</returns>
        public byte[] Read()
        {
            // Create empty array to hold incoming data
            var buffer = new byte[0];

            lock (ReadLock)
            {
                if (IsOpen)
                {
                    var waitCount = 0;
                    while (_serialPort.BytesToRead < 11)
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

                    _serialPort.Read(buffer, 0, 11);
                    _serialPort.DiscardInBuffer();
                }
            }

            return buffer;
        }

        #endregion
    }
}