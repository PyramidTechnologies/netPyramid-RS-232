
namespace Apex7000_BillValidator
{
    /// <summary>
    /// All errors reported by a Pyramid Bill acceptor
    /// </summary>
    public enum ErrorTypes
    {
        /// <summary>
        /// Slave message has an invalid checksum
        /// </summary>
        CheckSumError,

        /// <summary>
        /// Slave reports a bill jam
        /// </summary>
        BillJam,

        /// <summary>
        /// Slave rejected the note due to validation or feed failure
        /// </summary>
        BillReject,

        /// <summary>
        /// Slave reports cashbox is full
        /// </summary>
        CashboxFull,

        /// <summary>
        /// Slave reports that cashbox is missing
        /// </summary>
        CashboxMissing,

        /// <summary>
        /// Slave reports a suspected cheating attempt
        /// </summary>
        BillFish,

        /// <summary>
        /// Slave has sent an invalid command. Note this may occur on power up on some operating systems.
        /// </summary>
        InvalidCommand,

        /// <summary>
        /// Serial port communication timeout
        /// </summary>
        Timeout,

        /// <summary>
        /// Failed to write to slave
        /// </summary>
        WriteError,

        /// <summary>
        /// General error opening, reading, or writing to port
        /// </summary>
        PortError
    }

    /// <summary>
    /// Only one state may be reported at a time. Note: Stacked and returned are really
    /// events but RS-232 spec put them in the State byte so we do to.
    /// </summary>
    [System.Flags]
    public enum States : byte
    {
        Idle = 1,
        Accepting = 2,
        Escrowed = 4,
        Stacking = 8,
        Stacked = 16,
        Returning = 32,
        Returned = 64
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
