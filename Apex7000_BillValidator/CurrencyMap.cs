using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator
{
    /// <summary>
    /// Describes how to translate the RS-232 bill index to an actiual denomination
    /// </summary>
    public interface CurrencyMap
    {
        /// <summary>
        /// Return the denomination value for the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        int GetDenomFromIndex(int index);
    }
}
