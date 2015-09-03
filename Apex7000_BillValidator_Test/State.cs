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
    internal enum ClearTypes
    {
        All,
        Events,
        States
    }

    partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static SolidColorBrush inactive = new SolidColorBrush(Colors.LightGray);
        private static SolidColorBrush activeError = new SolidColorBrush(Colors.LightPink);
        private static SolidColorBrush activeEvent = new SolidColorBrush(Colors.LightGreen);
        private static SolidColorBrush activeState = new SolidColorBrush(Colors.LightBlue);

        #region Fields
        private string _portName = "";
        private States _state = States.Offline;
        private Events _event = Events.None;

        private bool _isConnected = false;
        #endregion


        #region Properties
        public string PortName
        {
            get { return _portName;  }
            set
            {
                _portName = value;
                NotifyPropertyChanged("PortName");
            }
        }

        public States State
        {
            get { return _state; }
            set
            {
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

                    });
         
                _state = value;
                NotifyPropertyChanged("State");
            }
        }

        public Events Event
        {
            get { return _event; }
            set
            {
                DoOnUIThread(() =>
                {
                    if ((_event & (Events.Returned | Events.Stacked | Events.BillRejected | Events.Cheated)) != 0)
                    {
                        clearTags(ClearTypes.All);
                    }
                });

                switch (value)
                {
                    case Events.BillRejected:
                        setEvent(btnRejected);
                        break;
                    case Events.Cheated:
                        setEvent(btnCheated);
                        break;
                    case Events.PowerUp:
                        setEvent(btnPup);
                        break;
                    case Events.Returned:
                        setEvent(btnReturned);
                        break;
                    case Events.Stacked:
                        setEvent(btnStacked);
                        break;

                    default:
                        setEvent(null);
                        break;
                }

                _event = value;
            }
        }

        public bool IsEscrowMode
        {
            get 
            {
                if (config != null)
                    return config.IsEscrowMode;
                else
                    return false;
            }
            set
            {
                if (config != null)
                {
                    config.IsEscrowMode = value;
                    NotifyPropertyChanged("IsEscrowMode");
                }
            }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                NotifyPropertyChanged("IsConnected");
                NotifyPropertyChanged("IsNotConnected");
            }
        }

        public bool IsNotConnected
        {
            get { return !IsConnected; }
        }

        #endregion

        /// <summary>
        /// Sets the state tag as active while clearing all other state tags
        /// </summary>
        /// <param name="target"></param>
        private void setState(Button target)
        {
            DoOnUIThread(() =>
            {
                //clearTags(typeof(States));
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
                if (target != null)
                {
                    target.Background = activeState;

                }
                else
                    clearTags(ClearTypes.Events);
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
                clearTags(ClearTypes.All);
                target.Background = activeError;
                setState(btnDisabled);
            });
        }

        /// <summary>
        /// Resets all state tags back to lightGrey. Must be called from UI thread.
        /// </summary>
        private void clearTags(ClearTypes type)
        {                     
            var tag = "";
            IEnumerable<Button> stateTags = StateMachine.Children.OfType<Button>();
            foreach (Button b in stateTags)
            {
                // lol. if the button has a tag, return it as a string. Otherwise tag == empty string.
                tag = (b.Tag ?? null) == null ? "" : b.Tag.ToString();

                switch (type)
                {
                    case ClearTypes.All:
                        b.Background = inactive;
                        break;
                    case ClearTypes.Events:
                        if (tag.Equals("event"))
                            b.Background = inactive;
                        break;
                    case ClearTypes.States:
                        if (tag.Equals("state"))
                            b.Background = inactive;
                        break;
                }
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
