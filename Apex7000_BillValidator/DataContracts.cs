using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

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

    public struct Response
    {
        public static readonly byte Idle = 0x01;
        public static readonly byte Accepting = 0x02;
        public static readonly byte Escrow = 0x04;
        public static readonly byte Stacking = 0x08;
        public static readonly byte Returned = 0x20;

        public static readonly byte CassetteRemoved = 0x00;
    }

    public struct Request
    {
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
        public static int getDenomFromByte(byte b, CultureInfo currentCulture)
        {
            if (currentCulture != null)
            {
                var r = new RegionInfo(currentCulture.LCID);
                string region = r.TwoLetterISORegionName;

                CurrencyMap m = new CurrencyMap();
                var currencyMaps = m.GetType().GetFields().ToList();
                foreach (var cm in currencyMaps)
                {
                    if (cm.Name == region)
                    {

                        foreach(var kvp in (Dictionary<byte, int>)cm.GetValue(null))
                        {
                            if (b == kvp.Key)
                                return kvp.Value;
                        }
                        break;
                    }
                }
            }
            else
            {
                currentCulture = new CultureInfo("en-US");
                return getDenomFromByte(b, currentCulture);
            }

            return 0;

        }
    }
}
