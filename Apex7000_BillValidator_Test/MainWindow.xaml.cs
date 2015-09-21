using PyramidNETRS232;
using System.Windows;

namespace PyramidNETRS232_TestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PyramidAcceptor validator;
        private RS232Config config;



        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }



        /// Simple UI listeners

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (validator != null)
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

        private void AvailablePorts_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            AvailablePorts.ItemsSource = PyramidAcceptor.GetAvailablePorts();
        }

        private void AvailablePorts_Loaded(object sender, RoutedEventArgs e)
        {
            AvailablePorts.ItemsSource = PyramidAcceptor.GetAvailablePorts();
        }

        private void ed_Changed(object sender, RoutedEventArgs e)
        {
            // Avoids npe on startup
            if (config == null)
                return;

            int mask = 0;

            // Could be done with data bindings but why bother when bits are so much fun?
            mask |= chk1.IsChecked.Value ? 1 << 0 : 0;
            mask |= chk2.IsChecked.Value ? 1 << 1 : 0;
            mask |= chk3.IsChecked.Value ? 1 << 2 : 0;
            mask |= chk4.IsChecked.Value ? 1 << 3 : 0;
            mask |= chk5.IsChecked.Value ? 1 << 4 : 0;
            mask |= chk6.IsChecked.Value ? 1 << 5 : 0;
            mask |= chk7.IsChecked.Value ? 1 << 6 : 0;

            config.EnableMask = (byte)mask;
        }

        private void sldPoll_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Do not allow updating until we're connected
            if (config == null || txtPoll == null)
            {
                return;
            }


            int val = (int)e.NewValue;
            config.PollRate = val;
            txtPoll.Text = string.Format("{0}", val);

        } 

    }
    
}
