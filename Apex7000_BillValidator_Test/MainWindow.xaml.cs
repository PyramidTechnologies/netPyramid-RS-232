using Apex7000_BillValidator;
using System;
using System.Globalization;
using System.Windows;

namespace Apex7000_BillValidator_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ApexValidator validator;
        private RS232Config config;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Testing on CAN firmware using escrow mode
            config = new RS232Config("COM4", CultureInfo.CurrentCulture, true);
            validator = new ApexValidator(config);
            validator.PowerUp += validator_PowerUp;
            validator.OnEscrow += validator_OnEscrow;
            validator.OnCredit += validator_OnCredit;
            validator.OnBillStacked += validator_BillStacked;
            validator.OnError += validator_OnError;
            validator.OnCashboxAttached += validator_CashboxAttached;

            validator.Connect();
        }

        void validator_OnError(object sender, ErrorTypes type)
        {
            Console.WriteLine("Error has occured: {0}", type.ToString());
        }

        void validator_CashboxAttached(object sender, EventArgs e)
        {
            Console.WriteLine("Box Attached");
        }

        void validator_BillStacked(object sender, EventArgs e)
        {            
            Console.WriteLine("Bill Stacked");
        }

        void validator_OnEscrow(object sender, int denomination)
        {
            validator.Stack();
            Console.WriteLine("Escrowed ${0}", denomination);
        }

        private void validator_OnCredit(object sender, int denomination)
        {
            Console.WriteLine("Credited ${0}", denomination);
        }

        void validator_PowerUp(object sender, EventArgs e)
        {
            Console.WriteLine("Acceptor Powered Up");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.Write(config.GetDebugBuffer());
        }
    }
}
