using System;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        // States, not really events (byte 1)
        public event EventHandler IsIdling;
        public event EventHandler IsAccepting;
        // Escrow belongs here but it requires an arg so it is further down
        public event EventHandler IsStacking;
        public event EventHandler IsReturning;

        // Events that are reported in the state byte
        public event EventHandler OnBillStacked;
        public event EventHandler OnBillReturned;

        // True RS-232 "events" (byte 2)
        public event EventHandler OnCashboxAttached;

        // Errors, credit, other (byte 3)
        public event EventHandler OnPowerUp;

        /// <summary>
        /// Raised once a note has been successfully stacked.
        /// </summary>
        /// <param name="sender">Object that raised event</param>
        /// <param name="denomination">Index 1-7 of the denomination stacked. See
        /// you bill acceptors documentation for the corresponding dollar value.</param>
        public delegate void OnCreditEventHandler(object sender, int denomination);
        public event OnCreditEventHandler OnCredit;

        /// <summary>
        /// subscribe to this event to be notified of when and what denomination is in escrow.
        /// If you are running in escrow mode, you may then decide whether to stack or reject
        /// the note based upon the denomination.
        /// </summary>
        /// <param name="sender">Object that raised event</param>
        /// <param name="denomination">Index 1-7 of the denomination in escrow. See
        /// you bill acceptors documentation for the corresponding dollar value.</param>
        public delegate void IsEscrowedEventHandler(object sender, int denomination);
        public event IsEscrowedEventHandler IsEscrowed;

        public delegate void OnErrorEventHandler(object sender, ErrorTypes type);
        public event OnErrorEventHandler OnError;

        #region Private
        /// <summary>
        /// Safely handle event. If handler is null, event is ignored.
        /// </summary>
        /// <param name="eventInst"></param>
        private void HandleEvent(EventHandler eventInst)
        {
            HandleEvent(eventInst, null);
        }

        /// <summary>
        /// Safely handle event. If handler is null, event is ignored.
        /// </summary>
        /// <param name="eventInst"></param>
        private void HandleEvent(EventHandler eventInst, EventArgs e)
        {
            EventHandler exec = eventInst;
            if (exec != null)
            {
                exec(this, e);
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
                handler(this, e);
            }
        }

        /// <summary>
        /// Raised when a bill enters escrow. Only raised while in escrow mode!
        /// </summary>
        /// <param name="e"></param>
        protected virtual void NotifyEscrow(int e)
        {
            IsEscrowedEventHandler handler = IsEscrowed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raised when an exceptional state occurs.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void NotifyError(ErrorTypes e)
        {
            OnErrorEventHandler handler = OnError;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion
    }
}
