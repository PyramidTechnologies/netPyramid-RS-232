using System;
using System.Runtime.Serialization;

namespace PTI.Serial
{
    /// \internal
    internal enum ExceptionTypes
    {
        Timeout,
        WriteError,
        PortError,
        AccessError
    }

    /// \internal
    [SerializableAttribute]
    internal class PortException : Exception, ISerializable
    {
        public ExceptionTypes ErrorType { get; private set; }

        public PortException(ExceptionTypes type)
            : base() 
        {
            ErrorType = type;
        }

        public PortException(ExceptionTypes type, string message)
            : base(message)
        {
            ErrorType = type;
        }

        public PortException(ExceptionTypes type, string message, Exception inner)
            : base(message, inner)
        {
            ErrorType = type;
        }

        // This constructor is needed for serialization.
        protected PortException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
