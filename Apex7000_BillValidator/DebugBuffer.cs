using System;
using System.Text;

namespace Apex7000_BillValidator
{
    /// <summary>
    /// Helper entry for describing serial communication transactions
    /// </summary>
    public class DebugBufferEntry
    {
        private static DateTime epoch;
        /// <summary>
        /// Sets the timestamp epoch. All timestamps are relative to this value.
        /// </summary>
        internal static void SetEpoch()
        {
            epoch = DateTime.Now;
        }

        /// <summary>
        /// Creates a new entry and marks it as being sent master->slave
        /// </summary>
        /// <param name="data">byte[]</param>
        /// <returns>DebugBufferEntry</returns>
        internal static DebugBufferEntry AsMaster(byte[] data)
        {
            return new DebugBufferEntry(data, Flows.Master);
        }

        /// <summary>
        /// Creates a new entry and marks it as being sent slave->master
        /// </summary>
        /// <param name="data">byte[]</param>
        /// <returns>DebugBufferEntry</returns>
        internal static DebugBufferEntry AsSlave(byte[] data)
        {
            return new DebugBufferEntry(data, Flows.Slave);
        }

        private DebugBufferEntry(byte[] data, Flows flow)
        {
            var dt = (DateTime.Now - epoch);
            Timestamp = String.Format("{0}:{1}:{2}::{3}", dt.Hours, dt.Minutes, dt.Seconds, dt.Milliseconds);
            
            var now = DateTime.Now;
            RealTime = String.Format("{0}:{1}:{2}", dt.Minutes, dt.Seconds, dt.Milliseconds);
            Data = data;
            Flow = flow;

            if (Flow == Flows.Master)
                DecodedData = MasterCodex.ToMasterMessage(data).ToString();
            else
                DecodedData = SlaveCodex.ToSlaveMessage(data).ToString();
        }

        /// <summary>
        /// Byte[] data that was transmitted
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Hex formatted byte[] data as 0xHH format
        /// </summary>
        public string PrintableData
        {
            get
            {
                return ByteArrayToString(Data);
            }
        }

        /// <summary>
        /// Returns Master or Slave
        /// </summary>
        public Flows Flow { get; private set; }

        /// <summary>
        /// Retrurns minutes:seconds:milliseconds timestamp relative to epoch
        /// </summary>
        public String Timestamp { get; private set; }

        /// <summary>
        /// Returns the PC time the packet was collected
        /// </summary>
        public String RealTime { get; private set; }

        /// <summary>
        /// byte[] decoded into known RS-232 messages
        /// </summary>
        public String DecodedData { get; private set; }

        /// <summary>
        /// Returns Flow :: Data :: Timestamp
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} :: {1} :: {2}", Flow, PrintableData, Timestamp);
        }

        /// <summary>
        /// Convert byte[] to a single-byte hex formatted string
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2} ", b);
            return hex.ToString();
        }        
    }

    /// <summary>
    /// The origin of this debug entry
    /// </summary>
    public enum Flows {

        /// <summary>
        /// Sent by master
        /// </summary>
        Master,

        /// <summary>
        /// Sent by slave
        /// </summary>
        Slave
    }
}
