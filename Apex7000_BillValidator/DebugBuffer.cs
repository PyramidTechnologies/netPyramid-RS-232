using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator
{
    public class DataEntry
    {
        private Flows flows;
        private int p;

        public DataEntry(byte[] data, Flows flow, int p)
        {
            var dt = DateTime.Now;
            Timestamp = String.Format("{0}:{1}:{2}", dt.Minute, dt.Second, dt.Millisecond);
            Data = data;
            Flow = flow;
            ThreadID = p;
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

        public int ThreadID { get; private set; }

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
