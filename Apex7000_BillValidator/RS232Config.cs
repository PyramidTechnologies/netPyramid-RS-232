using OpenNETCF.IO.Ports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator
{
    public class RS232Config
    {

        // ms
        private static readonly int POLL_RATE = 200;
        // seconds
        private static readonly int SLAVE_DEAD_LIMIT = 10;

        private static readonly int DEBUG_BUFFER_SZ = 100;

        #region Fields
        // Integer poll rate between 50 and 5000 ms
        private int pollRate = POLL_RATE;

        private LinkedList<DebugBufferEntry> debugBuffer = new LinkedList<DebugBufferEntry>();

        // Used to map between reported value and actual currency denomination
        private CultureInfo currentCulture;
        private Dictionary<byte, int> currentCurrencyMap;
        #endregion

        public RS232Config(COMPort commPort)
        {
            new RS232Config(commPort.ToString(), CultureInfo.CurrentCulture, false);
        }

        public RS232Config(string commPort)
        {
            new RS232Config(commPort.ToString(), CultureInfo.CurrentCulture, false);
        }

        public RS232Config(COMPort commPort, CultureInfo culture, bool isEscrowMode)
        {
            new RS232Config(commPort.ToString(), currentCulture, isEscrowMode);
        }

        public RS232Config(string commPort, CultureInfo culture, bool isEscrowMode)
        {
            this.CommPortName = commPort;
            this.currentCulture = culture;
            this.IsEscrowMode = isEscrowMode;

            this.CashboxPresent = true;
            this.EscrowTimeout = DateTime.MinValue;

            //this.currentCurrencyMap = BillParser.getCurrencyMap(this.currentCulture);
        }

        public string GetDebugBuffer()
        {
            StringBuilder sb = new StringBuilder();
            foreach(DebugBufferEntry dbe in debugBuffer)
                sb.Append(dbe.ToString() + "\n");

            debugBuffer.Clear();
            return sb.ToString();
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
        /// Slaves last state
        /// </summary>
        public Response PreviousResponse { get; internal set; }

        /// <summary>
        /// Returns true if underlying port is connected
        /// </summary>
        public bool IsConnected { get; internal set; }
        #endregion       

        #region Internal State
        // State variables for tracking between events and states
        internal byte Ack {get; set; }

        // Track if we have already reported the cashbox state. We always
        // raise the cashbox missing event but report cashbox attached event
        // only once.
        internal bool CashboxPresent { get; set; }

        // If true, the slave is reporting that a note is in escrow
        internal bool IsEscrowed { get; set; }

        // Last reported credit from slave
        internal byte Credit { get; set; }

        // Additional feature: async flag to tell the slave
        // to ACCEPT or REJECT the note next time the master polls
        internal EscrowCommands EscrowCommand { get; set; }

        // Used in case we need to retransmit
        internal byte[] previouslySentMasterMsg { get; set; }

        // Track comm timeout from slave device
        internal DateTime EscrowTimeout { get; set; }
        internal int ReconnectAttempts { get; set; }

        internal void pushDebugEntry(DebugBufferEntry entry) 
        {
            debugBuffer.AddLast(entry);

            if (debugBuffer.Count > DEBUG_BUFFER_SZ)
                debugBuffer.Clear();
        }
        #endregion
    }
}
