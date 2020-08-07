﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Debug
{
    /// <summary>
    /// Validator for master data
    /// </summary>
    public class MasterDataValidator : BaseDataValidator
    {
        private static readonly List<string> Enables = new List<string> {"$1", "$2", "$5", "$10", "$20", "$50", "$100"};

        /// <summary>
        /// Creates a new master data validator
        /// </summary>
        /// <param name="rawData">Raw data received</param>
        public MasterDataValidator(byte[] rawData) : base(rawData)
        {
        }
        
        /// <summary>
        /// List of disables indicated by the Disable byte
        /// </summary>
        private List<string> Disabled { get; set; } = new List<string>();

        /// <summary>
        /// Checks the Disable/Enable byte of the data received and populates the Disables
        /// property with every disable that is parsed. Adds a validation message fragment
        /// indicating what disabled bills were parsed from the raw data.
        /// </summary>
        protected void CheckDisables()
        {
            var disables = new List<string>();
            for (int b = 0; b < Enables.Count; b++)
            {
                if ((Data[0] & 1 << b) == 1 << b)
                {
                    disables.Add(Enables[b]);
                }
            }

            Disabled = disables;

            if (Disabled.Any())
            {
                var disableString = $"Disables: {string.Join(",", Disabled)}";
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
            
            if (!CheckChecksum())
            {
                ValidationMessageFragments.Add("Bad checksum");
                return false;
            }
            
            CheckDisables();

            if ((Data[1] & 0b00000001) == 1)
            {
                ValidationMessageFragments.Add("Reserved bit set: Byte 0 Bit 0");
                return false;
            }

            var security = Data[1] & 0b00000010;
            var orientation1 = Data[1] & 0b00000100;
            var orientation2 = Data[1] & 0b00001000;
            
            if ((security > 0) | (orientation1 > 0) | (orientation2 > 0))
            {
                ValidationMessageFragments.Add("Reserved features are in use.");
                return false;
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
                ValidationMessageFragments.Add("Reserved bit set: byte 2");
                return false;
            }

            return true;

        }
    }
}