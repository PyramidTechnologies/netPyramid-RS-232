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

        private FixedObservableLinkedList<DebugBufferEntry> debugQueue;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            debugQueue = new FixedObservableLinkedList<DebugBufferEntry>(14);
            AvailablePorts.ItemsSource = ApexValidator.GetAvailablePorts();
        }
     
        /// <summary>
        /// Checks if the specified index is enabled by our checkboxes. This could also be a call to 
        /// a remote system or any other check-pass/fail process.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool checkEnableMask(int index)
        {
            switch(index)
            {
                case 1:
                    return chk1.IsChecked == true;
                case 2:
                    return chk2.IsChecked == true;
                case 3:
                    return chk3.IsChecked == true;
                case 4:
                    return chk4.IsChecked == true;
                case 5:
                    return chk5.IsChecked == true;
                case 6:
                    return chk6.IsChecked == true;
                case 7:
                    return chk7.IsChecked == true;

                default:
                    return false;
            }
        }
    }
    
}
