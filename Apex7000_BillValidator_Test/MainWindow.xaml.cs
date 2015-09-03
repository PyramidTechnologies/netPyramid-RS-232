using Apex7000_BillValidator;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Apex7000_BillValidator_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ApexValidator validator;
        private RS232Config config;

        private bool btnConnectedLock = false;

        private FixedObservableLinkedList<DebugBufferEntry> debugQueue;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            debugQueue = new FixedObservableLinkedList<DebugBufferEntry>(20);
            AvailablePorts.ItemsSource = ApexValidator.GetAvailablePorts();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // We're already connected
            if (btnConnectedLock)
            {
                validator.Close();
                btnConnect.Content = "Connect";
                btnConnectedLock = false;
            }
            else
            {
                btnConnectedLock = true;
                btnConnect.Content = "Disconnect";

                PortName = AvailablePorts.Text;
                if (string.IsNullOrEmpty(PortName))
                {
                    MessageBox.Show("Please select a port");
                    return;
                }

                // Testing on CAN firmware using escrow mode
                config = new RS232Config(PortName, true);
                validator = new ApexValidator(config);

                // Configure logging
                config.OnSerialData += config_OnSerialData;
                ConsoleLogger.ItemsSource = debugQueue;

                // Configure events and state (All optional)
                validator.OnEvent += validator_OnEvent;
                validator.OnStateChanged += validator_OnStateChanged;
                validator.OnError += validator_OnError;
                validator.OnCashboxAttached += validator_CashboxAttached;

                // Required if you are in escrow mode
                validator.OnEscrowed += validator_OnEscrow;
                validator.OnCredit += validator_OnCredit;

                // This starts the acceptor
                validator.Connect();
            }

            AvailablePorts.IsEnabled = !btnConnectedLock;
        }

        void config_OnSerialData(object sender, DebugBufferEntry entry)
        {
            DoOnUIThread(() =>
            {
                debugQueue.Add(entry);

                ConsoleLogger.ScrollIntoView(entry);

            });
        }
               
        void validator_OnStateChanged(object sender, States state)
        {
            if (state != States.Idling)
                Console.WriteLine("State: {0}", state);
            State = state;
        }

        void validator_OnEvent(object sender, Events e)
        {
            if(e != Events.None)
                Console.WriteLine("Event Occured: {0}", e);
            Event = e;
        }
        void validator_CashboxAttached(object sender, EventArgs e)
        {
            Console.WriteLine("Cashbox Attached");
            setState(btnCB);
        }

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

        private void validator_OnCredit(object sender, int denomination)
        {
            if (currencyMap.ContainsKey(denomination))
            {
                var credited = AddCredit(denomination);
                if (credited > 0)
                    Console.WriteLine("Credited ${0}", credited);
                else
                    Console.WriteLine("Failed to credit: {0}", denomination);
            }
        }

        void validator_OnEscrow(object sender, int index)
        {
            if (index != 3)
            {
                validator.Stack();
                State = States.Escrowed;

                if (currencyMap.ContainsKey(index))
                    Console.WriteLine("Escrowed ${0}", currencyMap[index]);
                else
                    Console.WriteLine("Unknown denomination index: {0}", index);
            }
            else
            {
                validator.Reject();

                if (currencyMap.ContainsKey(index))
                    Console.WriteLine("Rejected ${0}", currencyMap[index]);
                else
                    Console.WriteLine("Unknown denomination index: {0}", index);
            }

        }       
    }
    
}
