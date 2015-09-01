using System;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        // States, not really events (byte 1)
        public event EventHandler OnIdling;
        public event EventHandler OnAccepting;
        public event EventHandler OnEscrowed;
        public event EventHandler OnStacking;
        public event EventHandler OnReturning;

        // Events that are reported in the state byte
        public event EventHandler OnBillStacked;
        public event EventHandler OnBillReturned;

        // True RS-232 "events" (byte 2)
        public event EventHandler OnCashboxAttached;

        // Errors, credit, other (byte 3)
        public event EventHandler PowerUp;

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
        public delegate void OnEscrowEventHandler(object sender, int denomination);
        public event OnEscrowEventHandler OnEscrow;

        public delegate void OnErrorEventHandler(object sender, ErrorTypes type);
        public event OnErrorEventHandler OnError;


        private void HandleEvent(EventHandler eventInst)
        {
            HandleEvent(eventInst, null);
        }

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
            OnEscrowEventHandler handler = OnEscrow;
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
    }
}
