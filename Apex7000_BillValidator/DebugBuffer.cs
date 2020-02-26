namespace PyramidNETRS232
{
    using System;
    using System.Text;

    /// <summary>
    ///     Helper entry for describing serial communication transactions
    /// </summary>
    public class DebugBufferEntry
    {
        private static DateTime epoch;

        private DebugBufferEntry(byte[] data, Flows flow)
        {
            var dt = DateTime.Now - epoch;
            Timestamp = $"{dt.Hours}:{dt.Minutes}:{dt.Seconds}::{dt.Milliseconds}";

            var now = DateTime.Now;
            RealTime = $"{dt.Minutes}:{dt.Seconds}:{dt.Milliseconds}";
            Data = data;
            Flow = flow;

            DecodedData = Flow == Flows.Master ? MasterCodex.ToMasterMessage(data).ToString() : SlaveCodex.ToSlaveMessage(data).ToString();
        }

        /// <summary>
        ///     Byte[] data that was transmitted
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        ///     Hex formatted byte[] data as 0xHH format
        /// </summary>
        public string PrintableData => ByteArrayToString(Data);

        /// <summary>
        ///     Returns Master or Slave
        /// </summary>
        public Flows Flow { get; }

        /// <summary>
        ///     Retrurns minutes:seconds:milliseconds timestamp relative to epoch
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        ///     Returns the PC time the packet was collected
        /// </summary>
        public string RealTime { get; }

        /// <summary>
        ///     byte[] decoded into known RS-232 messages
        /// </summary>
        public string DecodedData { get; }

        /// <summary>
        ///     Sets the timestamp epoch. All timestamps are relative to this value.
        /// </summary>
        internal static void SetEpoch()
        {
            epoch = DateTime.Now;
        }

        /// <summary>
        ///     Creates a new entry and marks it as being sent master->slave
        /// </summary>
        /// <param name="data">byte[]</param>
        /// <returns>DebugBufferEntry</returns>
        internal static DebugBufferEntry AsMaster(byte[] data)
        {
            return new DebugBufferEntry(data, Flows.Master);
        }

        /// <summary>
        ///     Creates a new entry and marks it as being sent slave->master
        /// </summary>
        /// <param name="data">byte[]</param>
        /// <returns>DebugBufferEntry</returns>
        internal static DebugBufferEntry AsSlave(byte[] data)
        {
            return new DebugBufferEntry(data, Flows.Slave);
        }

        /// <summary>
        ///     Returns Flow :: Data :: Timestamp
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Flow} :: {PrintableData} :: {Timestamp}";
        }

        /// <summary>
        ///     Convert byte[] to a single-byte hex formatted string
        /// </summary>
        /// <param name="ba"></param>
        /// <returns></returns>
        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
            {
                hex.AppendFormat("{0:X2} ", b);
            }

            return hex.ToString();
        }
    }

    /// <summary>
    ///     The origin of this debug entry
    /// </summary>
    public enum Flows
    {
        /// <summary>
        ///     Sent by master
        /// </summary>
        Master,

        /// <summary>
        ///     Sent by slave
        /// </summary>
        Slave
    }
}