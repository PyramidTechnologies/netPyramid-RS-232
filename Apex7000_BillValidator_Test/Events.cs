using Apex7000_BillValidator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Apex7000_BillValidator_Test
{
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
            // We're already connected
            if (IsConnected)
            {
                validator.Close();
                btnConnect.Content = "Connect";
                IsConnected = false;
            }
            else
            {
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

                // Configure logging
                config.OnSerialData += config_OnSerialData;
                ConsoleLoggerMaster.ItemsSource = debugQueueMaster;
                ConsoleLoggerSlave.ItemsSource = debugQueueSlave;

                // Configure events and state (All optional)
                validator.OnEvent += validator_OnEvent;
                validator.OnStateChanged += validator_OnStateChanged;
                validator.OnError += validator_OnError;
                validator.OnCashboxAttached += validator_CashboxAttached;

                // Required if you are in escrow mode
                validator.OnEscrowed += validator_OnEscrow;


                // Technically optional but you probably want this event
                validator.OnCredit += validator_OnCredit;

                // This starts the acceptor
                validator.Connect();
            }
        }

        /// <summary>
        /// On receipt of a debug entry, add the entry to our UI console
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="entry"></param>
        void config_OnSerialData(object sender, DebugBufferEntry entry)
        {
            DoOnUIThread(() =>
            {
                if (entry.Flow == Flows.Master)
                {
                    debugQueueMaster.Add(entry);

                    ConsoleLoggerMaster.ScrollIntoView(entry);
                }
                else
                {
                    debugQueueSlave.Add(entry);

                    ConsoleLoggerSlave.ScrollIntoView(entry);
                }
            });
        }

        /// <summary>
        /// When the slave raises a state update, animated our UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        void validator_OnStateChanged(object sender, States state)
        {
            State = state;
        }


        /// <summary>
        /// When the slave raises events, update our UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void validator_OnEvent(object sender, Events e)
        {
            Event = e;
        }

        /// <summary>
        /// When the cashbox is in the attached state, set the UI
        /// accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void validator_CashboxAttached(object sender, EventArgs e)
        {
            Console.WriteLine("Cashbox Attached");
            setState(btnCB);
        }


        /// <summary>
        /// When the slave raises an error, log or report the the specified error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        void validator_OnError(object sender, Errors type)
        {
            Console.WriteLine("Error has occured: {0}", type.ToString());


            switch (type)
            {
                case Errors.CashboxMissing:
                    setError(btnCB);
                    break;
                case Errors.ChecksumError:
                    // TODO
                    break;
                case Errors.InvalidCommand:
                    // TODO
                    break;
                case Errors.PortError:
                    // TODO
                    break;
                case Errors.Timeout:
                    // TODO
                    break;
                case Errors.WriteError:
                    // TODO
                    break;
            }

        }

        /// <summary>
        /// On credit, add the val to our bill bank.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="denomination"></param>
        private void validator_OnCredit(object sender, int denomination)
        {
            if (currencyMap.ContainsKey(denomination))
            {
                if (denomination > 0)
                    Console.WriteLine("Credited ${0}", AddCredit(denomination));
                else
                    Console.WriteLine("Failed to credit: {0}", denomination);
            }
        }

        /// <summary>
        /// If in escrow mode, check
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index"></param>
        void validator_OnEscrow(object sender, int index)
        {
            DoOnUIThread(() =>
                {

                    string action = "";

                    // If bill is enabled by our mask, stack the note
                    bool isEnabled = checkEnableMask(index);
                    if (isEnabled)
                    {
                        // Pass Escrow state to UI
                        State = States.Escrowed;
                        action = "Escrowed";
                        validator.Stack();
                    }
                    else
                    {
                        action = "Rejected";
                        validator.Reject();
                    }


                    if (currencyMap.ContainsKey(index))
                        Console.WriteLine("{0} ${1}", action, currencyMap[index]);
                    else
                        Console.WriteLine("{0} Unknown denomination index: {1}", action, index);


                });
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if(validator != null)
            {
                validator.RequestReset();
            }
        }


        private void chkEscrowMode_Checked(object sender, RoutedEventArgs e)
        {
            IsEscrowMode = true;
        }

        private void chkEscrowMode_Unchecked(object sender, RoutedEventArgs e)
        {
            IsEscrowMode = false;
        } 
    }
}
