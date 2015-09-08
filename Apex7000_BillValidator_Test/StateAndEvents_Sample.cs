using Apex7000_BillValidator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Apex7000_BillValidator_Test
{
    /// <summary>
    /// Show how to handle the state and event message if you have subscribed to them
    /// </summary>
    partial class MainWindow
    {
       
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
                case Errors.FailedToOpenPort:
                    DoOnUIThread(()=>MessageBox.Show("Failed to open Port"));
                    break;
            }

        }
      
    }
}
