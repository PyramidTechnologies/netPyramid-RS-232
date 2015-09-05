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
            AvailablePorts.ItemsSource = ApexValidator.GetAvailablePorts();
        }

        private void AvailablePorts_Loaded(object sender, RoutedEventArgs e)
        {
            AvailablePorts.ItemsSource = ApexValidator.GetAvailablePorts();
        } 

    }
    
}
