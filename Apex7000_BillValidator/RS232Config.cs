using System;

namespace Apex7000_BillValidator
{
    public class RS232Config
    {

        // default pollrate in ms
        private static readonly int POLL_RATE = 100;

        #region Fields
        // Integer poll rate between 50 and 5000 ms
        private int pollRate = POLL_RATE;
        #endregion

        public RS232Config(string commPort)
        {
            new RS232Config(commPort.ToString(), false);
        }

        public RS232Config(string commPort, bool isEscrowMode)
        {
            this.CommPortName = commPort;
            this.IsEscrowMode = isEscrowMode;
        }
        #region Properties

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
        /// String name of the comm port (What the OS calls it)
        /// </summary>
        public string CommPortName { get; private set; }

        /// <summary>
        /// Escrow mode allows you to manually call Stack() or Reject() on each
        /// escrowed note. If false, we stack any valid note automatically.
        /// </summary>
        public bool IsEscrowMode { get; set; }

        /// <summary>
        /// Slave's last state
        /// </summary>
        public States PreviousState { get; internal set; }

        /// <summary>
        /// Slave's last events
        /// </summary>
        public Events PreviousEvents { get; internal set; }

        /// <summary>
        /// Returns true if the communication thread is running normally
        /// </summary>
        public bool IsRunning { get; internal set; }

        /// <summary>
        /// Gets or sets the timeout for escrow mode. By default, we wait indefinately but
        /// you may configure this to a non-zero value to enable escrow timeouts. This has the effect
        /// of sending a reject message to the acceptor once timeout occurs.
        /// </summary>
        public int EscrowTimeoutSeconds { get; set; }
        #endregion       
    }
}
