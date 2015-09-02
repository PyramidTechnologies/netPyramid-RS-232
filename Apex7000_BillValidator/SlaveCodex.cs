using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator
{
    /// <summary>
    /// Used internally to quickly translate between bytes and the string meaning
    /// </summary>
    internal class SlaveCodex
    {
        /// <summary>
        /// RS-232 mixed a couple of events in with state
        /// </summary>
        public enum Byte0 : byte
        {
            Idling = 1,
            Accepting = 2,
            Escrowed = 4,
            Stacking = 8,
            Stacked = 16,
            Returned = 32,
            Returned = 64
        }

        enum Byte1 : byte
        {
            Cheated = 1,
            BillRejected = 2,
            BillJammed = 4,
            StackerFull = 8,
            StackerPresent = 16,
            Reserved1 = 32,         // Set to 0
            Reserved2 = 64          // Set to 0
        }

        enum Byte2 : byte
        {
            PowerUp = 1,
            InvalidCommand = 2,
            Failure = 4,
            C1 = 8,
            C2 = 16,
            C3 = 32,
            Reserved = 64           // Set to 0
        }


        // enum Byte 3 Reserved - all bits must be 0

        // enum Byte 4 Model number - (00-7FH)

        // enum Byte 5 Firmware Rev - (00-7FH)

    }
}
