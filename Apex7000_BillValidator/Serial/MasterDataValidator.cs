namespace PyramidNETRS232.Serial
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Validator for master data
    /// </summary>
    internal class MasterDataValidator : BaseDataValidator
    {
        /// <summary>
        /// List of disables indicated by the Disable byte
        /// </summary>
        private List<string> Disabled { get; set; } = new List<string>();

        /// <summary>
        /// Checks the Disable/Enable byte of the data received and populates the Disables
        /// property with every disable that is parsed. Adds a validation message fragment
        /// indicating what disabled bills were parsed from the raw data.
        /// </summary>
        private void CheckDisables()
        {
            var enables = new List<string> {"$1", "$2", "$5", "$10", "$20", "$50", "$100"};
            
            for (int b = 0; b < enables.Count; b++)
            {
                if ((Data[0] & 1 << b) == 1 << b)
                {
                    Disabled.Add(enables[b]);
                }
            }
            
            if (Disabled.Any())
            {
                var disableString = $"Disables: {string.Join(",", Disabled.ToArray())}";
                ValidationMessageFragments.Add(disableString);
            }
        }
        
        /// <inheritdoc/>
        protected override bool ValidateSerialData()
        {
            ValidationMessageFragments.Add("MASTER");
            if (RawData is null)
            {
                ValidationMessageFragments.Add("Raw data is null");
                return false;
            }

            if (RawData.Length < 2 || RawData.Length != RawData[1])
            {
                ValidationMessageFragments.Add("Message received too short");
                return false;
            }
            
            if ((RawData[2] & 0x10) != 0x10)
            {
                ValidationMessageFragments.Add("Master did not set master bit");
                return false;
            }

            ValidationMessageFragments.Add($"ACK {RawData[2] & 0b00001111}");
            
            Data = RawData.Skip(3).Take(3).ToArray();

            var valid = true;
            
            if (!CheckChecksum())
            {
                ValidationMessageFragments.Add("Bad checksum");
                valid = false;
            }
            
            CheckDisables();

            if ((Data[1] & 0b00000001) == 1)
            {
                ValidationMessageFragments.Add("Reserved bit set: Byte 0 Bit 0");
                valid = false;
            }

            var security = Data[1] & 0b00000010;
            var orientation1 = Data[1] & 0b00000100;
            var orientation2 = Data[1] & 0b00001000;
            
            if ((security > 0) | (orientation1 > 0) | (orientation2 > 0))
            {
                ValidationMessageFragments.Add("Reserved bits of data byte 1 are not all 0.");
                valid = false;
            }

            var escorw_enabled = $"escrow={(Data[1] & 0b00010000) == 0b00010000}";
            ValidationMessageFragments.Add(escorw_enabled);

            var action = string.Empty;

            if ((Data[1] & 0b00100000) == 0b00100000)
            {
                action = "STACK!";
            }
            else
            {
                if ((Data[1] & 0b01000000) == 0b01000000)
                {
                    action = "RETURN!";
                }
            }
           
            ValidationMessageFragments.Add(action);

            if (Data[2] != 0)
            {
                ValidationMessageFragments.Add("Reserved data byte 2 is not set to zero.");
                valid = false;
            }

            return valid;

        }
    }
}