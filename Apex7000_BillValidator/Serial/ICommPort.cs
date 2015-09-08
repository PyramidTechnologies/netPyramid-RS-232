// Copyright (c) 2014 All Right Reserved, Pyramid Technologies, Inc. http://pyramidacceptors.com
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// <author>Cory Todd</author>
// <email>cory@pyramidacceptors.com</email>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PTI.Serial
{
    /// <summary>
    /// Defines the contract new implementations of a serial port must adhere to.
    /// </summary>
    public interface ICommPort : IDisposable
    {

        /// <summary>
        /// Return true if the underlying serial port is open
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Return the OS name of the underlying port
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or Sets the timeout in milliseconds for a read operation.
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        /// Attempts to open the underlying serial port using the currently
        /// configured state. Returns true if port is successfully opened.
        /// </summary>
        /// <returns>bool</returns>
        bool Connect();

        /// <summary>
        /// Attempts to safely close the underling serial port and base stream.
        /// If the close operation completes without issue, bool true is returned.
        /// </summary>
        /// <returns>bool</returns>
        bool Disconnect();

        /// <summary>
        /// Writes the byte[] data to this port. If the port is not open, misconfigured,
        /// or if there is a physical connection issue, exceptions may arise.
        /// </summary>
        /// <param name="data">byte[]</param>
        void Write(byte[] data);

        /// <summary>
        /// Reads all available data from this port. If no data is received withint
        /// ReadTimeout milliseconds, a timeout exception will be raised.
        /// </summary>
        /// <returns>byte[]</returns>
        byte[] Read();          
    }

}
