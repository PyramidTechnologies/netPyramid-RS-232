 namespace PyramidNETRS232.Serial
{
    using System.Collections.Generic;
    using System.Linq;
    
    /// <summary>
    /// Base class for defining serial data validation
    /// </summary>
    public abstract class BaseDataValidator
    {
        private byte[] _rawData;
        private bool _isValid;
        
        /// <summary>
        /// Data portion of the raw bytes received
        /// </summary>
        public byte[] Data { get; protected set; }
        
        /// <summary>
        /// Raw bytes received
        /// </summary>
        public byte[] RawData {
            get
            {
                return _rawData;
            }
            set
            {
                _rawData = value;
                _isValid = ValidateSerialData();
            }
        }

        /// <summary>
        /// true iff the data received does not violate protocol
        /// </summary>
        public virtual bool IsValid => _isValid;
        
        /// <summary>
        /// Returns true if the calculated checksum matches the checksum
        /// sent with the raw bytes
        /// </summary>
        /// <returns></returns>
        protected bool CheckChecksum()
        {
            if (RawData.Length < 3)
            {
                return false;
            }
            
            var actual = 0;
            var expected = RawData.Last();

            foreach (var b in RawData.Skip(1).Take(RawData.Length - 3))
            {
                actual ^= b;
            }

            return actual == expected;
        }

        /// <summary>
        /// List of messages that can be added to during the validation process.
        /// <see cref="ValidateSerialData"/>
        /// </summary>
        protected List<string> ValidationMessageFragments { get; set; } = new List<string>();
        
        /// <summary>
        /// Message indicating any protocol violations, states, errors and events
        /// parsed from the raw data
        /// </summary>
        public string ValidationMessage => string.Join(",", ValidationMessageFragments.ToArray());

        /// <summary>
        /// Validates the serial data received
        /// </summary>
        /// <returns>true if the serial data is valid and does not violate
        /// the protocol</returns>
        protected abstract bool ValidateSerialData();

    }
}