namespace PyramidNETRS232
{
    using System;

    /// <summary>
    ///     Used internally to quickly translate between bytes and the string meaning
    /// </summary>
    /// \internal
    internal static class SlaveCodex
    {
        private const SlaveMessage StateMask = (SlaveMessage) 0x40C2F;
        private const SlaveMessage EventMask = (SlaveMessage) 0x30350;
        private const SlaveMessage CbOkayMask = (SlaveMessage) 0x001000;
        private const SlaveMessage CreditMask = (SlaveMessage) 0x380000;

        internal static SlaveMessage ToSlaveMessage(byte[] message)
        {
            if (message.Length != 11)
            {
                return SlaveMessage.InvalidCommand;
            }

            var combined = (message[5] << 16) |
                           (message[4] << 8) |
                           message[3];
            return (SlaveMessage) combined;
        }

        internal static States GetState(SlaveMessage message)
        {
            // Clear non-state bits
            message &= StateMask;

            if ((message & SlaveMessage.Failure) == SlaveMessage.Failure)
            {
                return States.AcceptorFailure;
            }

            if ((message & SlaveMessage.StackerFull) == SlaveMessage.StackerFull)
            {
                return States.StackerFull;
            }

            if ((message & SlaveMessage.BillJammed) == SlaveMessage.BillJammed)
            {
                return States.BillJammed;
            }

            if ((message & SlaveMessage.Returning) == SlaveMessage.Returning)
            {
                return States.Returning;
            }

            if ((message & SlaveMessage.Stacking) == SlaveMessage.Stacking)
            {
                return States.Stacking;
            }

            if ((message & SlaveMessage.Escrowed) == SlaveMessage.Escrowed)
            {
                return States.Escrowed;
            }

            if ((message & SlaveMessage.Accepting) == SlaveMessage.Accepting)
            {
                return States.Accepting;
            }

            if ((message & SlaveMessage.Idling) == SlaveMessage.Idling)
            {
                return States.Idling;
            }

            return States.Offline;
        }

        internal static Events GetEvents(SlaveMessage message)
        {
            message &= EventMask;
            var result = Events.None;

            if ((message & SlaveMessage.Stacked) == SlaveMessage.Stacked)
            {
                result |= Events.Stacked;
            }

            if ((message & SlaveMessage.Returned) == SlaveMessage.Returned)
            {
                result |= Events.Returned;
            }

            if ((message & SlaveMessage.Cheated) == SlaveMessage.Cheated)
            {
                result |= Events.Cheated;
            }

            if ((message & SlaveMessage.BillRejected) == SlaveMessage.BillRejected)
            {
                result |= Events.BillRejected;
            }

            if ((message & SlaveMessage.PowerUp) == SlaveMessage.PowerUp)
            {
                result |= Events.PowerUp;
            }

            if ((message & SlaveMessage.InvalidCommand) == SlaveMessage.InvalidCommand)
            {
                result |= Events.InvalidCommand;
            }

            return result;
        }

        internal static bool IsCashboxPresent(SlaveMessage message)
        {
            message &= CbOkayMask;

            return (message & SlaveMessage.StackerPresent) == SlaveMessage.StackerPresent;
        }

        internal static int GetCredit(SlaveMessage message)
        {
            message &= CreditMask;

            return (int) message >> 19;
        }

        // enum Byte 3 Reserved - all bits must be 0

        // enum Byte 4 Model number - (00-7FH)
        internal static string GetModelNumber(byte byte4)
        {
            return $"{byte4}";
        }

        // enum Byte 5 Firmware Rev - (00-7FH)
        internal static string GetFirmwareRevision(byte byte5)
        {
            return $"{byte5}";
        }

        /// <summary>
        ///     RS-232 mixed a couple of events in with state
        /// </summary>
        [Flags]
        internal enum SlaveMessage
        {
            // Byte 0 - bit 0
            Idling = 1 << 0, // State
            Accepting = 1 << 1, // State
            Escrowed = 1 << 2, // State
            Stacking = 1 << 3, // State
            Stacked = 1 << 4, // Event
            Returning = 1 << 5, // State
            Returned = 1 << 6, // Event

            // Ignore 8th bit in 7-bit RS-232
            X1 = 1 << 7,

            // Byte1 - bit 0
            Cheated = 1 << 8, // Event
            BillRejected = 1 << 9, // Event
            BillJammed = 1 << 10, // State
            StackerFull = 1 << 11, // State
            StackerPresent = 1 << 12, // Ephemeral
            Reserved1 = 1 << 13, // Set to 0
            Reserved2 = 1 << 14, // Set to 0

            // Ignore 8th bit in 7-bit RS-232
            X2 = 1 << 15,

            // Byte2 - bit 0
            PowerUp = 1 << 16, // Event
            InvalidCommand = 1 << 17, // Event
            Failure = 1 << 18, // State
            C1 = 1 << 19, // Credit bit 1
            C2 = 1 << 20, // Credit bit 2
            C3 = 1 << 21, // Credit bit 3
            Reserved = 1 << 23, // Set to 0

            // Ignore 8th bit in 7-bit RS-232
            X3 = 1 << 24
        }
    }
}