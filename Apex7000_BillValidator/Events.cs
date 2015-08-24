using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        public event EventHandler PowerUp;
        public event EventHandler BeginEscrow;
        public event EventHandler BillStacked;
        public event EventHandler ClearLastError;
        public event EventHandler CashboxRemoved;
        public event EventHandler CashboxAttached;

        public delegate void OnEscrowEventHandler(object sender, int denomination);
        public event OnEscrowEventHandler OnEscrow;

        public delegate void OnErrorEventHandler(object sender, ErrorTypes type);
        public event OnErrorEventHandler OnError;
    }
}
