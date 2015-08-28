using Apex7000_BillValidator;
using System;
using System.Windows;

namespace Apex7000_BillValidator_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ApexValidator validator;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //validator = new ApexValidator(COMPort.COM10, "en-US");
            validator = new ApexValidator(COMPort.COM2);
            validator.PowerUp += validator_PowerUp;
            validator.OnEscrow += validator_OnEscrow;
            validator.BillStacked += validator_BillStacked;
            validator.OnError += validator_OnError;
            validator.CashboxAttached += validator_CashboxAttached;
            validator.CashboxRemoved += validator_CashboxRemoved;

            validator.Connect();
        }

        void validator_OnError(object sender, ErrorTypes type)
        {
            Console.WriteLine("Error has occured: {0}", type.ToString());
        }

        void validator_CashboxRemoved(object sender, EventArgs e)
        {
            Console.WriteLine("Box removed");
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
        }

        void validator_PowerUp(object sender, EventArgs e)
        {
            
        }
    }
}
