using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Apex7000_BillValidator
{
    public enum COMPort
    {
        COM1,
        COM2,
        COM3,
        COM4,
        COM5,
        COM6,
        COM7,
        COM8,
        COM9,
        COM10,
        COM11,
        COM12
    }

    public enum ErrorTypes
    {
        MotorFailure,
        CheckSumError,
        BillJam,
        BillRemove,
        StackerOpen,
        CashboxFull,
        CashboxMissing,
        SensorProblem,
        BillFish,
        StackerProblem,
        BillReject,
        InvalidCommand,
        StackFailure,
        Timeout,
        WriteError,
        PortError
    }

    [System.Flags]
    public enum Response : byte
    {
        Idle = 1,
        Accepting = 2,
        Escrow = 4,
        Stacking = 8,
        Stacked = 16,
        Returning = 32,
        Returned = 64
    }

    public enum EscrowCommands
    {
        None,
        Pending,
        Stack,
        Reject
    }

    public struct Request
    {
                                //   basic message   0      1      2      3      4      5    6      7
                                //                   start, len,  ack, bills,escrow,resv'd,end, checksum
        public static readonly byte[] BaseMessage = { 0x02, 0x08, 0x10, 0x7F, 0x10, 0x00, 0x03 };
        public static readonly byte[] Ack = { 0x02, 0x08, 0x11, 0x7F, 0x10, 0x00, 0x03 };
        public static readonly byte[] Escrow = { 0x02, 0x08, 0x11, 0x7F, 0x10, 0x00, 0x03 };
        public static readonly byte[] Stack = { 0x02, 0x08, 0x11, 0x7F, 0x30, 0x00, 0x03 };
        public static readonly byte[] Reject = { 0x02, 0x08, 0x11, 0x7F, 0x50, 0x00, 0x03 };
    }

    public struct CurrencyMap
    {
        public static readonly Dictionary<byte, int> US = new Dictionary<byte, int>() { { 0x01, 1 }, { 0x03, 5 }, { 0x04, 10 }, { 0x05, 20 }, { 0x06, 50 }, { 0x07, 100 } };
        public static readonly Dictionary<byte, int> CA = new Dictionary<byte, int>() { { 0x01, 5 }, { 0x02, 10 }, { 0x03, 20 }};

    }

    public class BillParser
    {
        public static object getCurrencyMap(CultureInfo currentCulture)
        {
            if (currentCulture != null)
            {
                var r = new RegionInfo(currentCulture.LCID);
                string region = r.TwoLetterISORegionName;

                CurrencyMap m = new CurrencyMap();                
                var currencyMaps = m.GetType().GetProperties();
                foreach (var cm in currencyMaps)
                {
                    if (cm.Name == region)
                    {

                        return Convert.ChangeType(cm, cm.PropertyType);
                    }
                }
            }
            else
            {
                currentCulture = new CultureInfo("en-US");
                return getCurrencyMap(currentCulture);
            }

            return CurrencyMap.US;
        }
    }
}
