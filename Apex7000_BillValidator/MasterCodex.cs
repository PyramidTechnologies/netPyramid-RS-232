
namespace PyramidNETRS232
{
    /// <summary>
    /// \internal
    /// </summary>
    internal class MasterCodex
    {
        // Mask out reserved and ignored bits
        private static MasterMessage relevanceMask = (MasterMessage)0xFE7F;

        [System.Flags]
        internal enum MasterMessage : int
        {
            // Byte0 - bit 0
            En1         = 1 << 0,
            En2         = 1 << 1,
            En3         = 1 << 2,
            En4         = 1 << 3,
            En5         = 1 << 4,
            En6         = 1 << 5,
            En7         = 1 << 6,
            
            // Ignore 8th bit
            x1              = 1 << 7,

            // Byte 1 - bit 0
            Reserved0       = 1 << 8,   // Set to 0
            Security        = 1 << 9,   // Set to 0
            Orientation1    = 1 << 10,  // Set to 0
            Orientation2    = 1 << 11,  // Set to 0
            Escrow          = 1 << 12,  // Set to 1 to enable
            Stack           = 1 << 13,   // In Escrow mode, set to 1 to stack
            Return          = 1 << 14,  // In Escrow mode, set to 1 to return

            // Ignore 8th bit
            x2              = 1 << 15,

            // Not part of spec, just added for decoding
            InvalidCommand  = 1 << 16
        }

        internal static MasterMessage ToMasterMessage(byte[] message)
        {
            if (message.Length != 8)
                return MasterMessage.InvalidCommand;

            int combined = (
                (message[4] << 8) |
                (message[3])
                );

            var result = (MasterMessage)(combined) & relevanceMask;
            return result;
        }
    }
}
