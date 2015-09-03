using Apex7000_BillValidator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Apex7000_BillValidator_Test
{
    partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static SolidColorBrush inactive = new SolidColorBrush(Colors.LightGray);
        private static SolidColorBrush activeError = new SolidColorBrush(Colors.LightPink);
        private static SolidColorBrush activeEvent = new SolidColorBrush(Colors.LightGreen);
        private static SolidColorBrush activeState = new SolidColorBrush(Colors.LightBlue);

        private string portName = "";
        private States state = States.BusyScanning;
        private bool isEscrowMode = false;

        public string PortName
        {
            get { return portName;  }
            set
            {
                portName = value;
                NotifyPropertyChanged("PortName");
            }
        }

        public States State
        {
            get { return state; }
            set
            {
                // Do not bother updating if we've already set this flag
                if ((value & state) == state)
                    return;

                DoOnUIThread(() =>
                    {
                        switch (value)
                        {
                            case States.Idling:
                                setState(btnIdle);
                                break;
                            case States.Accepting:
                                setState(btnAccepting);
                                break;
                            case States.Escrowed:
                                setState(btnEscrowed);
                                break;
                            case States.Stacking:
                                setState(btnStacking);
                                break;
                            case States.Returning:
                                setState(btnReturning);
                                break;
                            case States.BillJammed:
                                setState(btnBillJammed);
                                break;
                            case States.StackerFull:
                                setState(btnStackerFull);
                                break;
                            case States.AcceptorFailure:
                                setState(btnFailure);
                                break;
                        }

                        state = value;
                        NotifyPropertyChanged("State");
                    });
            }
        }

        public bool EscrowMode
        {
            get { return isEscrowMode;  }
            set
            {
                isEscrowMode = value;
                NotifyPropertyChanged("EscrowMode");
            }
        }

        /// <summary>
        /// Sets the state tag as active while clearing all other state tags
        /// </summary>
        /// <param name="target"></param>
        private void setState(Button target)
        {
            DoOnUIThread(() =>
            {
                clearStates();
                target.Background = activeState;
            });
        }

        /// <summary>
        /// Sets an event as active
        /// </summary>
        /// <param name="target"></param>
        private void setEvent(Button target)
        {
            DoOnUIThread(() =>
            {
                target.Background = activeState;
            });
        }

        /// <summary>
        /// Sets an error as active
        /// </summary>
        /// <param name="target"></param>
        private void setError(Button target)
        {
            DoOnUIThread(() =>
            {
                clearStates();
                target.Background = activeError;
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
                b.Background = inactive;
            }

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
