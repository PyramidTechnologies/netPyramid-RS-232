
namespace Apex7000_BillValidator
{
    public enum States : byte
    {
        BusyScanning,

        Idling,
        Accepting,
        Escrowed,
        Stacking,
        Returning,
        BillJammed,
        StackerFull,
        AcceptorFailure
    }

    [System.Flags]
    public enum Events : byte
    {
        None            = 0,

        Stacked         = 1 << 0,
        Returned        = 1 << 1,
        Cheated         = 1 << 2,
        BillRejected    = 1 << 3,
        PowerUp         = 1 << 4,
        InvalidCommand  = 1 << 5
    }

    [System.Flags]
    public enum Errors : byte
    {
        None            = 0,

        Timeout         = 1 << 0,
        WriteError      = 1 << 1,
        PortError       = 1 << 2,
        CashboxMissing  = 1 << 3,
        ChecksumError   = 1 << 4,
        InvalidCommand  = 1 << 5
    }

    /// <summary>
    /// Issue these commands while in escrow mode.
    /// </summary>
    internal enum EscrowCommands
    {
        /// <summary>
        /// No commands are active or pending
        /// </summary>
        None,

        /// <summary>
        /// A command has been issued and is currently being acted upon. This may take multiple
        /// message cycles to clear.
        /// </summary>
        Pending,

        /// <summary>
        /// Issues the stack command during the next message loop
        /// </summary>
        Stack,

        /// <summary>
        /// Issues the reject command during the next message loop
        /// </summary>
        Reject
    }

    /// <summary>
    /// Base message from which all packets are derived
    /// </summary>
    public struct Request
    {
                                //   basic message   0      1      2      3      4      5    6      7
                                //                   start, len,  ack, bills,escrow,resv'd,end, checksum
        public static readonly byte[] BaseMessage = { 0x02, 0x08, 0x10, 0x7F, 0x10, 0x00, 0x03 };

        // TODO
        public static readonly byte[] ResetTarget = { };
    }  
}
