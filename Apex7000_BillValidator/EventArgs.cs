namespace PyramidNETRS232
{
    using System;

    /// <summary>
    ///     Properties of a state change event
    /// </summary>
    public class StateChangedArgs : EventArgs
    {
        /// <summary>
        ///     A state changed argument describes the current, single state.
        /// </summary>
        /// <param name="state"></param>
        public StateChangedArgs(States state)
        {
            State = state;
        }

        /// <summary>
        ///     Most recently reported state of slave
        /// </summary>
        public States State { get; }
    }

    /// <summary>
    ///     Properties of an event change event
    /// </summary>
    public class EventChangedArgs : EventArgs
    {
        /// <summary>
        ///     An event change argument describes an event or multiple events
        ///     simultaneously.
        /// </summary>
        /// <param name="e"></param>
        public EventChangedArgs(Events e)
        {
            Event = e;
        }

        /// <summary>
        ///     Most recently reported event or events
        /// </summary>
        public Events Event { get; }
    }

    /// <summary>
    ///     Properties of an error event
    /// </summary>
    public class ErrorArgs : EventArgs
    {
        /// <summary>
        ///     An error event argument describes why an operation failed
        /// </summary>
        /// <param name="e"></param>
        public ErrorArgs(Errors e)
        {
            Error = e;
        }

        /// <summary>
        ///     Error reported by RS-232 library
        /// </summary>
        public Errors Error { get; }
    }

    /// <summary>
    ///     Properties of a credit event
    /// </summary>
    public class CreditArgs : EventArgs
    {
        /// <summary>
        ///     A credit event argument describes a credit event.
        /// </summary>
        /// <param name="index">Index of note to credit</param>
        public CreditArgs(int index)
        {
            Index = index;
        }

        /// <summary>
        ///     Index of note for which credit should be issues. Index
        ///     is an integer between 1 and 7 inclusive.
        ///     Denomination mapping depends on the Apex 7000 firmware but
        ///     here is the USD map:
        ///     1: $1
        ///     2: $2 (not used)
        ///     3: $5
        ///     4: $10
        ///     5: $20
        ///     6: $50
        ///     7: $100
        /// </summary>
        public int Index { get; }
    }

    /// <summary>
    ///     Properties of an escrow event
    ///     <seealso cref="RS232Config.IsEscrowMode" />
    /// </summary>
    public class EscrowArgs : EventArgs
    {
        /// <summary>
        ///     Escrow event args describe an escrow event.
        /// </summary>
        /// <seealso cref="RS232Config.IsEscrowMode" />
        /// <param name="index">Index of note in escrow</param>
        public EscrowArgs(int index)
        {
            Index = index;
        }

        /// <summary>
        ///     Index of note that is in escrow position. Index
        ///     is an integer between 1 and 7 inclusive.
        ///     Denomination mapping depends on the Apex 7000 firmware but
        ///     here is the USD map:
        ///     1: $1
        ///     2: $2 (not used)
        ///     3: $5
        ///     4: $10
        ///     5: $20
        ///     6: $50
        ///     7: $100
        ///     <seealso cref="RS232Config.IsEscrowMode" />
        /// </summary>
        public int Index { get; }
    }

    /// <summary>
    ///     Properties of a debug entry event
    /// </summary>
    public class DebugEntryArgs : EventArgs
    {
        /// <summary>
        ///     DebugEntry argument describes a debug event
        /// </summary>
        /// <param name="entry">DebugBufferEntry describing serial data tx/rx</param>
        public DebugEntryArgs(DebugBufferEntry entry)
        {
            Entry = entry;
        }

        /// <summary>
        ///     Describes data being debugged. Includes raw byte[] along with
        ///     a translation and some timing information.
        /// </summary>
        public DebugBufferEntry Entry { get; }
    }
}