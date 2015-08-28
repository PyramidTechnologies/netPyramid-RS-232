using System;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        public void Stack()
        {
            // Set flag to accept
            this.escrowCommand = EscrowCommands.Stack;
        }

        public void Reject()
        {
            // Set flag to reject
            this.escrowCommand = EscrowCommands.Reject;
        }
    }
}
