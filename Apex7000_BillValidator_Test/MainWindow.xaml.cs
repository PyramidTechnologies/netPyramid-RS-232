using Apex7000_BillValidator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media;

namespace Apex7000_BillValidator_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ApexValidator validator;
        private RS232Config config;

        private static Dictionary<int, int> currencyMap = new Dictionary<int, int>();

        private static SolidColorBrush lightGray = new SolidColorBrush(Colors.LightGray);
        private static SolidColorBrush red = new SolidColorBrush(Colors.Red);
        private static SolidColorBrush green = new SolidColorBrush(Colors.Green);
        private static SolidColorBrush activeTag = new SolidColorBrush(Colors.Blue);

        private Dictionary<int, long> cashbox = new Dictionary<int, long>();


        // Configure our map
        static MainWindow()
        {
            currencyMap.Add(1, 1);
            currencyMap.Add(2, 2);
            currencyMap.Add(3, 5);
            currencyMap.Add(4, 10);
            currencyMap.Add(5, 20);
            currencyMap.Add(6, 50);
            currencyMap.Add(7, 100);
        }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            Loaded += MainWindow_Loaded;

        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Testing on CAN firmware using escrow mode
            config = new RS232Config("COM4", CultureInfo.CurrentCulture, true);
            validator = new ApexValidator(config);

            validator.OnPowerUp += validator_PowerUp;
            validator.IsIdling += validator_IsIdling;
            validator.IsAccepting += validator_IsAccepting;
            validator.IsReturning += validator_IsReturning;
            validator.IsStacking +=validator_IsStacking;

            validator.IsEscrowed += validator_OnEscrow;
            validator.OnCredit += validator_OnCredit;

            validator.OnBillStacked += validator_BillStacked;
            validator.OnBillReturned += validator_OnBillReturned;
            
            validator.OnError += validator_OnError;
            validator.OnCashboxAttached += validator_CashboxAttached;

            validator.Connect();

            setupStateDiagram();
        }

        #region Events
        void validator_PowerUp(object sender, EventArgs e)
        {
            Console.WriteLine("Acceptor Powered Up");
            setActive(btnPup);
        }

        void validator_OnBillReturned(object sender, EventArgs e)
        {
            setActive(btnReturned);
        }

        void validator_BillStacked(object sender, EventArgs e)
        {
            Console.WriteLine("Bill Stacked");
            setActive(btnStacked);
        }

        void validator_CashboxAttached(object sender, EventArgs e)
        {
            Console.WriteLine("Box Attached");
            setActive(btnCB);
        }

        void validator_OnError(object sender, ErrorTypes type)
        {
            Console.WriteLine("Error has occured: {0}", type.ToString());


            switch (type)
            {
                case ErrorTypes.BillFish:
                    setError(btnCheated);
                    break;
                case ErrorTypes.BillJam:
                    setError(btnBillJammed);
                    break;
                case ErrorTypes.BillReject:
                    setError(btnRejected);
                    break;
                case ErrorTypes.CashboxFull:
                    setError(btnStackerFull);
                    break;
                case ErrorTypes.CashboxMissing:
                    setError(btnCB);
                    break;
                case ErrorTypes.CheckSumError:
                    // TODO
                    break;
                case ErrorTypes.InvalidCommand:
                    // TODO
                    break;
                case ErrorTypes.PortError:
                    // TODO
                    break;
                case ErrorTypes.Timeout:
                    // TODO
                    break;
                case ErrorTypes.WriteError:
                    // TODO
                    break;
            }

        }

        private void validator_OnCredit(object sender, int denomination)
        {
            if (currencyMap.ContainsKey(denomination))
            {
                var val = currencyMap[denomination];
                Console.WriteLine("Credited ${0}", AddCredit(val));
            }
        }
        #endregion

        #region States
        void validator_IsIdling(object sender, EventArgs e)
        {
            setActive(btnIdle);
        }

        void validator_IsAccepting(object sender, EventArgs e)
        {
            setActive(btnAccepting);
        }

        void validator_OnEscrow(object sender, int denomination)
        {
            validator.Stack();

            clearStates();
            DoOnUIThread(() => btnEscrowed.Background = activeTag);

            if (currencyMap.ContainsKey(denomination))
                Console.WriteLine("Escrowed ${0}", currencyMap[denomination]);
        }

        private void validator_IsStacking(object sender, EventArgs e)
        {
            setActive(btnStacking);
        }

        void validator_IsReturning(object sender, EventArgs e)
        {
            setActive(btnReturning);
        }
        #endregion

        private void setActive(Button target)
        {
            DoOnUIThread(() =>
            {
                clearStates();
                target.Background = activeTag;
            });
        }

        private void setError(Button target)
        {
            DoOnUIThread(() =>
            {
                clearStates();
                target.Background = red;
            });
        }  

        /// <summary>
        /// Resets all state tags back to lightGrey. Must be called from UI thread.
        /// </summary>
        private void clearStates()
        {

            IEnumerable<Button> stateTags = StateMachine.Children.OfType<Button>();
            foreach (Button b in stateTags)
            {
                b.Background = lightGray;
            }
  
        }

        private void setupStateDiagram()
        {
            var al = new ConnectingLine(LineDirections.Down, StateMachine, btnPup, btnDisabled);
            DoOnUIThread(() => StateMachine.Children.Add(al.Line));
        }

        private void DoOnUIThread(Action action)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(action);
            }
            else
            {
                action.Invoke();
            }
        }
    }
}
