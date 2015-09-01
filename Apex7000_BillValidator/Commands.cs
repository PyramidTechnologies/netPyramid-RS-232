using System;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        public void Stack()
        {
            // Set flag to accept
            config.EscrowCommand = EscrowCommands.Stack;
        }

        public void Reject()
        {
            // Set flag to reject
            config.EscrowCommand = EscrowCommands.Reject;
        }
    }
}
