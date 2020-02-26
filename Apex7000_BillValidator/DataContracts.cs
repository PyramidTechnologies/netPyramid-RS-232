namespace PyramidNETRS232
{
    using System;

    /// <summary>
    ///     The bill acceptor will pass through a series of "States" during bill processing. The acceptor will always
    ///     be in one single "State". If the acceptor is waiting for a bill insertion, it will report an "Idle" state
    ///     to the master. If the acceptor is reading a bill, it will report an "Accepting" state to the master. The change
    ///     from one state to another is called a transition.
    /// </summary>
    public enum States : byte
    {
        /// <summary>
        ///     No slave is currently attached to the master.
        /// </summary>
        Offline,

        /// <summary>
        ///     Slave reports normal, idle activity. Ready to accept notes.
        /// </summary>
        Idling,

        /// <summary>
        ///     Slave reports that it is currently pulling in a note.
        /// </summary>
        Accepting,

        /// <summary>
        ///     Slave reports that a note is in the escrow position. This position
        ///     is a physical location inside the acceptor that is too far for the
        ///     customer to pull back but not far enough to that it can't be returned
        ///     if necessary. This state is only report in Escrow Mode.
        ///     <seealso cref="RS232Config.IsEscrowMode" />
        /// </summary>
        Escrowed,

        /// <summary>
        ///     Slave reports that the note is in the process of being stacked.
        /// </summary>
        Stacking,

        /// <summary>
        ///     Slave reports that the note is in the process of being returned.
        /// </summary>
        Returning,

        /// <summary>
        ///     Slave reports that there is a note jam in the feed path that is
        ///     was not able to clear after a reasonable amount of time. This jam
        ///     may be located in the cashbox or the main bottom plate.
        /// </summary>
        BillJammed,

        /// <summary>
        ///     Slave reports that the cashbox is full and unable to stack anymore notes.
        ///     When this state is reported, the acceptor is considered out of service.
        ///     The front LEDs will be off and no notes can be inserted until the cashbox
        ///     is emptied $$$.
        /// </summary>
        StackerFull,

        /// <summary>
        ///     Slave reports a failure that is not a jam or cashbox related issue. This
        ///     could include motor failure or EEPROM/Flash memory failure.
        /// </summary>
        AcceptorFailure
    }


    /// <summary>
    ///     Slave acceptor may report a single-shot "Event" taking place. Multiple events
    ///     may be reported in a single message. Events are only reported one time and will
    ///     always accompany a state. If a message is retransmitted, the event will be reported
    ///     a second time but only becuase it was for a retransmission, not because the event
    ///     occured twice. If the slave has just stacked a bill in the cashbox, the slave will
    ///     report a "Stacked" event and since it is now waiting for another bill insertion,
    ///     it will also report an "Idle" state within the same message.
    /// </summary>
    [Flags]
    public enum Events : byte
    {
        /// <summary>
        ///     No events to report
        /// </summary>
        None = 0,

        /// <summary>
        ///     Note has successfully been added to the cashbox
        /// </summary>
        Stacked = 1 << 0,

        /// <summary>
        ///     Note has successfully been returned to the patron
        /// </summary>
        Returned = 1 << 1,

        /// <summary>
        ///     Cheat attempt suspected. The Apex 7000 will return a note
        ///     to the patron if a cheat is suspected.
        /// </summary>
        Cheated = 1 << 2,

        /// <summary>
        ///     Note was not recognized as valid OR was recognized as valid but
        ///     disabled note.
        /// </summary>
        BillRejected = 1 << 3,

        /// <summary>
        ///     The slave is powering up while this event is being reported. No
        ///     commands sent by the master to the slave will be acted upon until
        ///     the power up event is over.
        /// </summary>
        PowerUp = 1 << 4,

        /// <summary>
        ///     Slave reports that the last message received from the master was invalid.
        /// </summary>
        InvalidCommand = 1 << 5
    }

    /// <summary>
    ///     Errors reported by this library
    /// </summary>
    [Flags]
    public enum Errors : byte
    {
        /// <summary>
        ///     Default error type - nothing is wrong
        /// </summary>
        None = 0,

        /// <summary>
        ///     Timed out reading from slave
        /// </summary>
        Timeout = 1 << 0,

        /// <summary>
        ///     Error occured while writing to slave. Possible
        ///     break in serial connection.
        /// </summary>
        WriteError = 1 << 1,

        /// <summary>
        ///     Unable to open, close, or write to port. May occur
        ///     if USB VCP is suddenly removed.
        /// </summary>
        PortError = 1 << 2,

        /// <summary>
        ///     Cashbox is not detected by slave
        /// </summary>
        CashboxMissing = 1 << 3,

        /// <summary>
        ///     Message from slave has an incorrect checksum. If you see this
        ///     along with InvalidCommands, it is likely that the serial connection
        ///     is damaged.
        /// </summary>
        ChecksumError = 1 << 4,

        /// <summary>
        ///     The last message received from the slave contains 1 or more invalid messages.
        ///     If you see this along with ChecksumErrors, it is likely that the serial connection
        ///     is damaged.
        /// </summary>
        InvalidCommand = 1 << 5,

        /// <summary>
        ///     Usually occurs when the target port is already open. May also occur
        ///     on some virtual null modems.
        /// </summary>
        FailedToOpenPort = 1 << 6
    }

    /// <summary>
    ///     Issue these commands while in escrow mode.
    /// </summary>
    internal enum EscrowCommands
    {
        /// <summary>
        ///     No commands are active or pending
        /// </summary>
        None,

        /// <summary>
        ///     Issues the stack command during the next message loop
        /// </summary>
        Stack,

        /// <summary>
        ///     Issues the reject command during the next message loop
        /// </summary>
        Reject,

        /// <summary>
        ///     Escrow message needs to be sent to client
        /// </summary>
        Notify,

        /// <summary>
        ///     Escrow message has been sent to client, awaiting stack or return command
        /// </summary>
        Awaiting,

        /// <summary>
        ///     Client has acted on the escrow command, escrow events will be raised on next poll loop
        /// </summary>
        Acknowledged
    }

    /// <summary>
    ///     Base message from which all packets are derived
    /// </summary>
    internal struct Request
    {
        //   basic message   0      1      2      3      4      5    6      7
        //                   start, len,  ack, bills,escrow,resv'd,end, checksum
        internal static readonly byte[] BaseMessage = {0x02, 0x08, 0x60, 0x7F, 0x10, 0x00, 0x03};

        internal static readonly byte[] ResetTarget = {0x02, 0x08, 0x61, 0x7f, 0x7f, 0x7f, 0x03};
    }
}