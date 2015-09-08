
namespace Apex7000_BillValidator
{
    public enum States : byte
    {
        Offline,

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

        /// <summary>
        /// Timed out reading from slave
        /// </summary>
        Timeout         = 1 << 0,

        /// <summary>
        /// Error occured while writing to slave. Possible
        /// break in serial connection.
        /// </summary>
        WriteError      = 1 << 1,

        /// <summary>
        /// Unable to open, close, or write to port. May occur
        /// if USB VCP is suddenly removed.
        /// </summary>
        PortError       = 1 << 2,

        /// <summary>
        /// Cashbox is not detected by slave
        /// </summary>
        CashboxMissing  = 1 << 3,

        /// <summary>
        /// Message from slave has an incorrect checksum. If you see this
        /// along with InvalidCommands, it is likely that the serial connection
        /// is damaged.
        /// </summary>
        ChecksumError   = 1 << 4,

        /// <summary>
        /// The last message received from the slave contains 1 or more invalid messages.
        /// If you see this along with ChecksumErrors, it is likely that the serial connection
        /// is damaged.
        /// </summary>
        InvalidCommand  = 1 << 5,

        // Usually occurs when the target port is already open.
        // May also occur on some virtual null modems.
        FailedToOpenPort = 1 << 6
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
        public static readonly byte[] ResetTarget = { 0x02, 0x08, 0x61, 0x7f, 0x7f, 0x7f, 0x03 };
    }  
}
