using Apex7000_BillValidator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Apex7000_BillValidator_Test
{
    /// <summary>
    /// This class shows how to instantiate and register events for the RS-232 bill acceptor
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// Occurs when users clicks connect. Once in the connected state, the 
        /// buttons becomes a disconnect button. Clicking in the disconnected state
        /// will connect, clicking in the connected will disconnect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // We're already connected - don't connect again!
            if (IsConnected)
            {
                validator.Close();
                btnConnect.Content = "Connect";
                IsConnected = false;

                return;
            }

            // Instantiate a validator and register for all the handlers we need
            IsConnected = true;
            btnConnect.Content = "Disconnect";

            PortName = AvailablePorts.Text;
            if (string.IsNullOrEmpty(PortName))
            {
                MessageBox.Show("Please select a port");
                return;
            }

            // Create a new instance using the specified port and in escrow mode
            config = new RS232Config(PortName, IsEscrowMode);
            config.EscrowTimeoutSeconds = 12;
            validator = new ApexValidator(config);

            // Configure logging - see DebugData_Sample.cs
            config.OnSerialData += config_OnSerialData;
            ConsoleLoggerMaster.ItemsSource = debugQueueMaster;
            ConsoleLoggerSlave.ItemsSource = debugQueueSlave;

            // Configure events and state (All optional) - see StateAndEvents_Sample.cs
            validator.OnEvent += validator_OnEvent;
            validator.OnStateChanged += validator_OnStateChanged;
            validator.OnError += validator_OnError;
            validator.OnCashboxAttached += validator_CashboxAttached;

            // Required if you are in escrow mode - see CreditAndEscrow_Sample.cs
            validator.OnEscrowed += validator_OnEscrow;

            // Technically optional but you probably want this event - see CreditAndEscrow_Sample.cs
            validator.OnCredit += validator_OnCredit;

            // This starts the acceptor - REQUIRED!!
            validator.Connect();

        }
    }
}
