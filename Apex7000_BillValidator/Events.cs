using System;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        /// <summary>
        /// Raised when the acceptor reports any event. Events are transient
        /// in that they are only reported once to the master.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Events</param>
        public delegate void OnEventHandler(object sender, EventChangedArgs e);
        public event OnEventHandler OnEvent;

        /// <summary>
        /// Raised when the acceptor reports a state that is different from the 
        /// previously recorded state. Note: In escrow mode the Escrowed event
        /// will be raised as 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state">States</param>
        public delegate void OnStateChangeHandler(object sender, StateChangedArgs e);
        public event OnStateChangeHandler OnStateChanged;

        /// <summary>
        /// Raised by the master in the event that communication fails
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type">Errors</param>
        public delegate void OnErrorEventHandler(object sender, ErrorArgs e);
        public event OnErrorEventHandler OnError;

        /// <summary>
        /// Raised once a note has been successfully stacked.
        /// </summary>
        /// <param name="sender">Object that raised event</param>
        /// <param name="index">Index 1-7 of the denomination stacked. See
        /// you bill acceptors documentation for the corresponding dollar value.</param>
        public delegate void OnCreditEventHandler(object sender, CreditArgs e);
        public event OnCreditEventHandler OnCredit;

        /// <summary>
        /// subscribe to this event to be notified of when and what denomination is in escrow.
        /// If you are running in escrow mode, you may then decide whether to stack or reject
        /// the note based upon the denomination.
        /// </summary>
        /// <param name="sender">Object that raised event</param>
        /// <param name="index">Index 1-7 of the denomination in escrow. See
        /// you bill acceptors documentation for the corresponding dollar value.</param>
        public delegate void OsEscrowedEventHandler(object sender, EscrowArgs e);
        public event OsEscrowedEventHandler OnEscrowed;

        /// <summary>
        /// Raised when the cashbox is no longer detached
        /// </summary>
        public event EventHandler OnCashboxAttached;



        /// <summary>
        /// Subscribe to serial data received and transmission events. Useful for debugging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="entry"></param>
        public delegate void OnSerialDataHandler(object sender, DebugEntryArgs e);
        public event OnSerialDataHandler OnSerialData;


        #region Private
        /// <summary>
        /// Safely handle event. If handler is null, event is ignored.
        /// </summary>
        /// <param name="eventInst"></param>
        private void NotifyEvent(Events e)
        {
            OnEventHandler exec = OnEvent;
            if (exec != null)
            {
                exec(this, new EventChangedArgs(e));
            }
        }

        /// <summary>
        /// Safely handle state change. If handler is null, event is ignored.
        /// </summary>
        /// <param name="eventInst"></param>
        private void NotifyStateChange(States state)
        {
            OnStateChangeHandler exec = OnStateChanged;
            if (exec != null)
            {
                exec(this, new StateChangedArgs(state));
            }
        }

        /// <summary>
        /// Safely handle event. If handler is null, event is ignored.
        /// </summary>
        /// <param name="eventInst"></param>
        private void SafeEvent(EventHandler eventInst)
        {
            EventHandler exec = eventInst;
            if (exec != null)
            {
                exec(this, null);
            }
        }

 
        /// <summary>
        /// Raised when a note is completely stacked.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void NotifyCredit(int e)
        {
            OnCreditEventHandler handler = OnCredit;
            if (handler != null)
            {
                handler(this, new CreditArgs(e));
            }
        }

        /// <summary>
        /// Raised when a bill enters escrow. Only raised while in escrow mode!
        /// </summary>
        /// <param name="e"></param>
        protected virtual void NotifyEscrow(int e)
        {
            OsEscrowedEventHandler handler = OnEscrowed;
            if (handler != null)
            {
                handler(this, new EscrowArgs(e));
            }
        }

        /// <summary>
        /// Raised when an exceptional state occurs.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void NotifyError(Errors e)
        {
            OnErrorEventHandler handler = OnError;
            if (handler != null)
            {
                handler(this, new ErrorArgs(e));
            }
        }
        #endregion
    }
}
