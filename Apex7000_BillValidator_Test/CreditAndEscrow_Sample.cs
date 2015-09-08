﻿using Apex7000_BillValidator;
using System;

namespace Apex7000_BillValidator_Test
{
    /// <summary>
    /// This class demonstrates how to handle the OnCredit Event
    /// </summary>
    partial class MainWindow
    {


        /// <summary>
        /// If in escrow mode, check that we have the specified index enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="index"></param>
        private void validator_OnEscrow(object sender, int index)
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