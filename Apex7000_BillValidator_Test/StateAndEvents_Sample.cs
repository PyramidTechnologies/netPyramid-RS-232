using PyramidNETRS232;
using System;
using System.Windows;

namespace PyramidNETRS232_TestApp
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
        void validator_OnStateChanged(object sender, StateChangedArgs state)
        {
            State = state.State;
        }


        /// <summary>
        /// When the slave raises events, update our UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void validator_OnEvent(object sender, EventChangedArgs e)
        {
            Event = e.Event;
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
        void validator_OnError(object sender, ErrorArgs type)
        {
            Console.WriteLine("Error has occured: {0}", type.ToString());


            switch (type.Error)
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
