﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Debug
{
    /// <summary>
    /// Validator for slave data
    /// </summary>
    public class SlaveDataValidator : BaseDataValidator
    {
        private static readonly List<string> PossibleStates = 
            new List<string> {"idling", "accepting", "escrowed", "stacking", "stacked", "returning", "returned"};
        private static readonly List<string> PossibleEvents = 
            new List<string> {"cheated", "rejected", "jammed", "stacker full", "cassette present"};
        
        private static readonly List<int> Credit = 
            new List<int> {0, 1, 2, 5, 10, 20, 50, 100};
        
        /// <summary>
        /// Creates a new slave data validator
        /// </summary>
        /// <param name="rawData"></param>
        public SlaveDataValidator(byte[] rawData) : base(rawData)
        {
            
        }

        /// <summary>
        /// List of states indicated by the state byte
        /// </summary>
        public List<string> States { get; protected set;} = new List<string>();
        
        /// <summary>
        /// Flagged true if the acceptor is in no state.
        /// Having a 0 for the state byte itself is not
        /// indicative of errant data, however if the
        /// state byte is zero, errors must be flagged as well;
        /// i.e. Data[0] == 0 && Data[1] & 0b1110111 == 0
        /// </summary>
        protected bool IsNoState => !States.Any();

        /// <summary>
        /// List of events indicated by the event byte
        /// </summary>
        public List<string> Events { get; protected set;} = new List<string>();
        
        /// <summary>
        /// List of errors indicated by the error byte
        /// </summary>
        public List<string> Errors { get; protected set;} = new List<string>();
        
        /// <summary>
        /// Checks the state byte of the data received and populates the States
        /// property with every state that is parsed. Adds a validation message fragment
        /// indicating what states were parsed from the raw data.
        /// </summary>
        protected void CheckStates()
        {
            var slaveStates = new List<string>();
            for (int b = 0; b < PossibleStates.Count; b++)
            {
                if ((Data[0] & 1 << b) == 1 << b)
                {
                    slaveStates.Add(PossibleStates[b]);
                }
            }

            States = slaveStates;

            if (States.Any())
            {
                var stateString = $"States: {string.Join(",", States)}";
                ValidationMessageFragments.Add(stateString);
            }
        }

        /// <summary>
        /// Checks the event byte of the data received and populates the Events
        /// property with every event that is parsed. Adds a validation message fragment
        /// indicating what events were parsed from the raw data.
        /// </summary>
        protected void CheckEvents()
        {
            var slaveEvents = new List<string>();
            for (int b = 0; b < PossibleEvents.Count; b++)
            {
                if ((Data[1] & 1 << b) == 1 << b)
                {
                    slaveEvents.Add(PossibleEvents[b]);
                }
            }

            Events = slaveEvents;

            if (Events.Any())
            {
                var stateString = $"Events: {string.Join(",", Events)}";
                ValidationMessageFragments.Add(stateString);
            }
        }

        /// <summary>
        /// Checks the errors byte of the data received and populates the Errors
        /// property with every error that is parsed. Adds a validation message fragment
        /// indicating what errors were parsed from the raw data.
        /// </summary>
        protected void CheckErrors()
        {
            if ((Data[2] & 0b00000001) != 0)
            {
                Errors.Add("power up");
            }
            if ((Data[2] & 0b00000010) != 0)
            {
                Errors.Add("invalid command");
            }
            if ((Data[2] & 0b00000100) != 0)
            {
                Errors.Add("ba failed");
            }

            if (Errors.Any())
            {
                var errorString = $"Errors: {string.Join(",", Errors)}";
                ValidationMessageFragments.Add(errorString);
            }
        }
        
        /// <summary>
        /// Bill value that has been credited, if any
        /// </summary>
        protected string BillValue = string.Empty;

        /// <inheritdoc/>
        protected override bool ValidateSerialData()
        {
            ValidationMessageFragments.Add("SLAVE");
            if (RawData == null)
            {
                ValidationMessageFragments.Add("Raw data is null");
                return false;
            }

            if (RawData.Length == 0)
            {
                ValidationMessageFragments.Add("No response");
                return false;
            }

            // add the raw bytes being parsed to our message
            {
                var rawBytes = string.Empty;
                foreach (var b in RawData)
                {
                    rawBytes += "0x" + Convert.ToString(b, 16) + " ";
                }
            
                ValidationMessageFragments.Add($"Raw Bytes received: {rawBytes}");
            }

            if (RawData.Length < 2 || RawData.Length != RawData[1])
            {
                ValidationMessageFragments.Add("Message received too short");
                return false;
            }
            
            if ((RawData[2] & 0x20) != 0x20)
            {
                ValidationMessageFragments.Add("Slave did not set slave bit");
                return false;
            }

            ValidationMessageFragments.Add($"ACK {RawData[2] & 0b00001111}");
            Data = RawData.Skip(3).Take(6).ToArray();
            

            if (!CheckChecksum())
            {
                ValidationMessageFragments.Add("Bad checksum");
                return false;
            }

            CheckStates();

            if (IsNoState && ((Data[1] & 0b1110111) == 0))
            {
                ValidationMessageFragments.Add("Both state (byte 0) and error (byte 1) bytes are zero. " +
                                               "The state byte should only equal zero if there are error bits set.");
                return false;
            }
            
            CheckEvents();

            if ((Data[1] & 0b01100000) != 0)
            {
                var reservedBits = Convert.ToString(Data[1], 2);
                ValidationMessageFragments.Add($"Reserved event bits set in byte 1 0b{reservedBits}");
            }
            
            CheckErrors();

            if ((Data[2] & 0b00111000) != 0)
            {
                BillValue = $"Bill Value: {Credit[(Data[2] >> 3)]}";
                ValidationMessageFragments.Add(BillValue);
            }

            if ((Data[2] & 0b01000000)!= 0)
            {
                ValidationMessageFragments.Add("Slave set reserved bits Byte 2 bit 6");
                return false;
            }
            
            if (Data[3] != 0)
            {
                ValidationMessageFragments.Add("Slave set reserved byte 3");
                return false;
            }

            return true;
        }
    }
}