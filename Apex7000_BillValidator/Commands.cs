using System;

namespace Apex7000_BillValidator
{
    public partial class ApexValidator
    {
        public void Stack()
        {
            // Set flag to accept
            // We do not directly instruct the unit to stack as this may interfere
            // with any pending transmissions.
            config.EscrowCommand = EscrowCommands.Stack;
        }

        public void Reject()
        {
            // Set flag to reject
            // We do not directly instruct the unit to reject as this may interfere
            // with any pending transmissions.
            config.EscrowCommand = EscrowCommands.Reject;
        }
    }
}
