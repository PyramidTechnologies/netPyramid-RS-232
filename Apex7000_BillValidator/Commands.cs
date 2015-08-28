using System;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        public void Stack()
        {
            escrowTimeout = DateTime.MinValue;
            Write(Request.Stack);
        }

        public void Reject()
        {
            escrowTimeout = DateTime.MinValue;
            Write(Request.Reject);
        }
    }
}
