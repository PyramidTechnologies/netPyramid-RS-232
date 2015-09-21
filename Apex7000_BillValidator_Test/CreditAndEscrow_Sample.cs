using PyramidNETRS232;
using System;
using System.Windows;

namespace PyramidNETRS232_TestApp
{
    /// <summary>
    /// This class demonstrates how to handle the OnCredit Event
    /// </summary>
    partial class MainWindow
    {
        /// <summary>
        /// All of this mess is the make the the async manual accept/reject buttons work
        /// </summary>
        private static object manualLock = new object();
        private bool actionTaken = false;
        private bool enableManualButtons = false;
        public bool EnableManualButtons
        {
            get { return enableManualButtons; }
            set
            {
                enableManualButtons = value;
                NotifyPropertyChanged("EnableManualButtons");
            }
        }
        private int lastIndex = 0;

        /// <summary>
        /// If in escrow mode, check that we have the specified index enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index"></param>
        private void validator_OnEscrow(object sender, EscrowArgs e)
        {
            lastIndex = e.Index;

            DoOnUIThread(() =>
            {
                // Here you could also a call to check the user's account balance to make sure they're not
                // exceeding a specified amount. Remember, returns and rejects can be triggered by a few things:
                // 1) Reject : Invalid note
                // 2) Reject : Cheat attemp
                // 3) Return : Note disabled by E/D mask
                // 4) Return : Host manually send return message because a check failed (e.g. too much money on user account etc.)

                
                // If we're already taken action, clear the actionTaken flag. Otherwise enable our
                // manual buttons so an action can be taken.
                lock (manualLock)
                {
                    if (!actionTaken && lastIndex != 0)
                        EnableManualButtons = true;
                    else
                        actionTaken = false;
                }

            });
        }



        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            validator.Stack();


            if (currencyMap.ContainsKey(lastIndex))
                Console.WriteLine("Escrowed ${0}", currencyMap[lastIndex]);
            else
                Console.WriteLine("Escrowed Unknown denomination index: {0}", lastIndex);


            lock (manualLock)
            {
                lastIndex = 0;

                actionTaken = true;
                EnableManualButtons = false;
            }
        }



        private void btnReject_Click(object sender, RoutedEventArgs e)
        {

            validator.Reject();


            if (currencyMap.ContainsKey(lastIndex))
                Console.WriteLine("Rejected ${0}", currencyMap[lastIndex]);
            else
                Console.WriteLine("Rejected Unknown denomination index: {0}", lastIndex);

            lock (manualLock)
            {
                lastIndex = 0;
                actionTaken = true;
                EnableManualButtons = false;
            }
        }
      




        /// <summary>
        /// On credit, add the val to our bill bank.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="denomination"></param>
        private void validator_OnCredit(object sender, CreditArgs e)
        {
            var denomination = e.Index;
            if (currencyMap.ContainsKey(denomination))
            {
                if (denomination > 0)
                    Console.WriteLine("Credited ${0}", AddCredit(denomination));
                else
                    Console.WriteLine("Failed to credit: {0}", denomination);
            }
        }


        /// <summary>
        /// Checks if the specified index is enabled by our checkboxes. This could also be a call to 
        /// a remote system or any other check-pass/fail process.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool checkEnableMask(int index)
        {
            switch (index)
            {
                case 1:
                    return chk1.IsChecked.Value;
                case 2:
                    return chk2.IsChecked.Value;
                case 3:
                    return chk3.IsChecked.Value;
                case 4:
                    return chk4.IsChecked.Value;
                case 5:
                    return chk5.IsChecked.Value;
                case 6:
                    return chk6.IsChecked.Value;
                case 7:
                    return chk7.IsChecked.Value;

                default:
                    return false;
            }
        }
    }
}
