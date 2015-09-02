using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator
{
    public class DebugBufferEntry
    {
        public static DebugBufferEntry AsMaster(byte[] data)
        {
            return new DebugBufferEntry(data, Flows.Master);
        }

        public static DebugBufferEntry AsSlave(byte[] data)
        {
            return new DebugBufferEntry(data, Flows.Slave);
        }

        private DebugBufferEntry(byte[] data, Flows flow)
        {
            var dt = DateTime.Now;
            Timestamp = String.Format("{0}:{1}:{2}", dt.Minute, dt.Second, dt.Millisecond);
            Data = data;
            Flow = flow;
        }

        public byte[] Data { get; private set; }
        public string PrintableData
        {
            get
            {
                return ByteArrayToString(Data);
            }
        }
        public Flows Flow { get; private set; }

        public String Timestamp { get; private set; }

        public override string ToString()
        {
            return String.Format("{0} :: {1} :: {2}", Flow, PrintableData);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2} ", b);
            return hex.ToString();
        }        
    }

    public enum Flows {
        Master,
        Slave
    }
}
