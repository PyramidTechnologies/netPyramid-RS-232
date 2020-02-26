namespace PyramidNETRS232
{
    using System;

    public partial class PyramidAcceptor
    {
        /// <summary>
        ///     Raised when the acceptor reports any event. Events are transient
        ///     in that they are only reported once to the master.
        /// </summary>
        public event EventHandler<EventChangedArgs> OnEvent;

        /// <summary>
        ///     Notify subscribers of event(s). Events may be one or more Events.
        /// </summary>
        /// <param name="events"></param>
        internal virtual void NotifyEvent(Events events)
        {
            var handler = OnEvent;
            handler?.Invoke(this, new EventChangedArgs(events));
        }

        /// <summary>
        ///     Raised when the acceptor reports a state that is different from the
        ///     previously recorded state. Note: In escrow mode the Escrowed event
        ///     will be raised as
        /// </summary>
        public event EventHandler<StateChangedArgs> OnStateChanged;

        /// <summary>
        ///     Notify subsribers of state. State may be a transition or the current
        ///     state, repeated.
        /// </summary>
        /// <param name="state"></param>
        internal virtual void NotifyStateChange(States state)
        {
            var handler = OnStateChanged;
            handler?.Invoke(this, new StateChangedArgs(state));
        }

        /// <summary>
        ///     Raised by the master in the event that communication fails
        /// </summary>
        public event EventHandler<ErrorArgs> OnError;

        /// <summary>
        ///     Report errors to the subscriber.
        /// </summary>
        /// <param name="errors"></param>
        internal virtual void NotifyError(Errors errors)
        {
            var handler = OnError;
            handler?.Invoke(this, new ErrorArgs(errors));
        }

        /// <summary>
        ///     Raised once a note has been successfully stacked.
        /// </summary>
        public event EventHandler<CreditArgs> OnCredit;

        /// <summary>
        ///     Notify subscriber that credit should be issue.
        /// </summary>
        /// <param name="index">Integer value 1-7 indicating which bill should be credited</param>
        internal virtual void NotifyCredit(int index)
        {
            var handler = OnCredit;
            handler?.Invoke(this, new CreditArgs(index));
        }

        /// <summary>
        ///     subscribe to this event to be notified of when and what denomination is in escrow.
        ///     If you are running in escrow mode, you may then decide whether to stack or reject
        ///     the note based upon the denomination.
        /// </summary>
        public event EventHandler<EscrowArgs> OnEscrow;

        /// <summary>
        ///     Notify subscriber that a note is in escrow.
        /// </summary>
        /// <seealso cref="RS232Config.IsEscrowMode" />
        /// <param name="index">Integer value 1-7 indicating which bill is in escrow</param>
        internal virtual void NotifyEscrow(int index)
        {
            var handler = OnEscrow;
            handler?.Invoke(this, new EscrowArgs(index));
        }

        /// <summary>
        ///     Raised when the cashbox is no longer detached. This only be reported if the cashbox is first attached,
        ///     then missing.
        /// </summary>
        public event EventHandler OnCashboxAttached;


        /// <summary>
        ///     Subscribe to serial data received and transmission events. Useful for debugging.
        /// </summary>
        public event EventHandler<DebugEntryArgs> OnSerialData;

        /// <summary>
        ///     Notify subsriber that data is available for debugging
        /// </summary>
        /// <param name="entry"></param>
        internal virtual void NotifySerialData(DebugBufferEntry entry)
        {
            var handler = OnSerialData;
            handler?.Invoke(this, new DebugEntryArgs(entry));
        }

        /// <summary>
        ///     Safely handle event. If handler is null, event is ignored.
        /// </summary>
        /// <param name="eventInst">Instance of EventHandler to exectute</param>
        private void SafeEvent(EventHandler eventInst)
        {
            var exec = eventInst;
            exec?.Invoke(this, null);
        }
    }
}