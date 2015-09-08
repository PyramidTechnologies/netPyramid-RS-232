using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator
{
    public class StateChangedArgs : EventArgs
    {
        public StateChangedArgs(States state) { State = state; }

        public States State { get; private set; }
    }

    public class EventChangedArgs : EventArgs
    {
        public EventChangedArgs(Events e) { Event = e; }

        public Events Event { get; private set; }
    }

    public class ErrorArgs : EventArgs
    {
        public ErrorArgs(Errors e) { Error = e; }

        public Errors Error { get; private set; }
    }

    public class CreditArgs : EventArgs
    {
        public CreditArgs(int index) { Index = index; }

        public int Index { get; private set; }
    }

    public class EscrowArgs : EventArgs
    {
        public EscrowArgs(int index) { Index = index; }

        public int Index { get; private set; }
    }

    public class DebugEntryArgs : EventArgs
    {
        public DebugEntryArgs(DebugBufferEntry entry) { Entry = entry; }

        public DebugBufferEntry Entry { get; private set; }
    }
}
