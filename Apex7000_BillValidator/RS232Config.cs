namespace PyramidNETRS232
{
    using System;

    /// <summary>
    ///     Define the operating parameters of your bill acceptor
    /// </summary>
    public class RS232Config
    {
        // default pollrate in ms
        private const int DefaultPollRate = 100;
        private byte _enableMask = 0x7F;


        // Integer poll rate between 50 and 5000 ms
        private int _pollRate = DefaultPollRate;

        /// <summary>
        ///     Create a new configuration to use for the Apex7000 validator class. Defaults
        ///     to non-escrow mode.
        /// </summary>
        /// <seealso cref="RS232Config.IsEscrowMode" />
        /// <param name="commPort">String port name e.g. COM4</param>
        public RS232Config(string commPort) : this(commPort, false)
        {
        }

        /// <summary>
        ///     Create a new configuration to use for the Apex7000 validator class.
        /// </summary>
        /// <seealso cref="RS232Config.IsEscrowMode" />
        /// <param name="commPort">String port name e.g. COM4</param>
        /// <param name="isEscrowMode">bool true to enable escrow mode</param>
        public RS232Config(string commPort, bool isEscrowMode)
        {
            CommPortName = commPort;
            IsEscrowMode = isEscrowMode;
        }


        /// <summary>
        ///     Gets or sets the poll rate in milliseconds. The polled system is designed for the master to request
        ///     information from the slave at a periodic rate. The rate can be as slow as 5 seconds or as fast as
        ///     50 msec between each poll. The popular rate is fast since the overall system performance
        ///     (bills per minute accepted) will be slower at slower polling rates. While feeding the bill into the
        ///     acceptor, the acceptor will miss a few polls, because it is reading the bill and not servicing the
        ///     serial interface (Typical for acceptors using this protocol).
        /// </summary>
        /// <remarks>Default value is 100 ms</remarks>
        /// <value>Min: 50 Max: 5000</value>
        public int PollRate
        {
            get => _pollRate;
            set
            {
                // Allow floor of 50 ms, celing of 5 seconds
                if (value < 50 || value > 5000)
                {
                    throw new ArgumentOutOfRangeException(nameof(PollRate), "Minimum value is 50ms, maximum value is 5000 ms");
                }

                _pollRate = value;
            }
        }

        /// <summary>
        ///     String name of the comm port (What the OS calls it)
        /// </summary>
        public string CommPortName { get; }

        /// <summary>
        ///     Escrow mode allows you to manually call Stack() or Reject() on each
        ///     escrowed note. If false, we stack any valid note automatically.
        /// </summary>
        /// <remarks>Default value is false</remarks>
        public bool IsEscrowMode { get; set; }

        /// <summary>
        ///     Gets or sets the timeout for escrow mode. By default, we wait indefinately but
        ///     you may configure this to a non-zero value to enable escrow timeouts. This has the effect
        ///     of sending a reject message to the acceptor once timeout occurs.
        /// </summary>
        /// <remarks>Default value is 0 (disabled)</remarks>
        public int EscrowTimeoutSeconds { get; set; }

        /// <summary>
        ///     Bitwise enable disbale pattern. Each bit set to 1 corresponds to an enabled bill.
        ///     e.g. 0x7E (0b01111110) is all bill except the $1 are enabled. This value is limited
        ///     to 7-bits (0x7F) and any extra bits will be unset. 0xFF -> 0x7F
        /// </summary>
        /// <remarks>Default mask is 0x7F (all enabled)</remarks>
        public byte EnableMask
        {
            get => _enableMask;
            set => _enableMask = (byte) (0x7F & value);
        }
    }
}