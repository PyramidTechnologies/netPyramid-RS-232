using Apex7000_BillValidator;
using System;
using System.Runtime.Serialization;

namespace PTI.Serial
{
    public enum PortErrors
    {
        Timeout,
        WriteError,
        PortError,
        AccessError
    }


    [SerializableAttribute]
    public class PortException : Exception, ISerializable
    {
        public PortErrors ErrorType { get; private set; }

        public PortException(PortErrors type)
            : base() 
        {
            ErrorType = type;
        }

        public PortException(PortErrors type, string message)
            : base(message)
        {
            ErrorType = type;
        }

        public PortException(PortErrors type, string message, Exception inner)
            : base(message, inner)
        {
            ErrorType = type;
        }

        // This constructor is needed for serialization.
        protected PortException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
