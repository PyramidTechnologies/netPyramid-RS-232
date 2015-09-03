using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator
{
    class MasterCodex
    {
        internal enum MasterMessage : int
        {
            // Byte0 - bit 0
            Accept1         = 1 << 0,
            Accept2         = 1 << 1,
            Accept3         = 1 << 2,
            Accept4         = 1 << 3,
            Accept5         = 1 << 4,
            Accept6         = 1 << 5,
            Accept7         = 1 << 6,
            
            // Ignore 8th bit
            x1              = 1 << 7,

            // Byte 1 - bit 0
            Reserved0       = 1 << 8,   // Set to 0
            Security        = 1 << 9,   // Set to 0
            Orientation1    = 1 << 10,  // Set to 0
            Orientation2    = 1 << 11,  // Set to 0
            Escrow          = 1 << 12,  // Set to 1 to enable
            Stack           = 1 << 13,   // In Escrow mode, set to 1 to stack
            Return          = 1 << 15,  // In Escrow mode, set to 1 to return

            // Ignore 8th bit
            x2              = 1 << 16,

            // Not part of spec, just added for decoding
            InvalidCommand  = 1 << 17
        }

        internal static MasterMessage ToMasterMessage(byte[] message)
        {
            if (message.Length != 8)
                return MasterMessage.InvalidCommand;

            int combined = (
                (message[5] << 8) |
                (message[4])
                );
            return (MasterMessage)combined;
        }
    }
}
