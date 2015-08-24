using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
