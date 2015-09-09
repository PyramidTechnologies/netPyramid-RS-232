using PyramidNETRS232;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PyramidNETRS232_TestApp
{
    internal enum TagTypes
    {
        NonEmptyTags,
        Events,
        States
    }


    /// <summary>
    /// This class demonstrates some databinding techniques for illustrating state and events.
    /// </summary>
    partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static SolidColorBrush inactive = new SolidColorBrush(Colors.LightGray);
        private static SolidColorBrush cashboxOkay = new SolidColorBrush(Colors.LightYellow);
        private static SolidColorBrush activeError = new SolidColorBrush(Colors.LightPink);
        private static SolidColorBrush activeEvent = new SolidColorBrush(Colors.LightGreen);
        private static SolidColorBrush activeState = new SolidColorBrush(Colors.LightBlue);

        #region Fields
        private string _portName = "";
        private States _state = States.Offline;
        private Events _event = Events.None;

        private bool _isEscrowMode = false;
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
                        clearTags(TagTypes.NonEmptyTags);
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
                        Console.WriteLine("Powered Up");
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
                    return _isEscrowMode;
            }
            set
            {
                if (config != null)                
                    config.IsEscrowMode = value;                
                else                
                    _isEscrowMode = value;                
                NotifyPropertyChanged("IsEscrowMode");
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
                if (target == btnCB)
                    target.Background = cashboxOkay;
                else                
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
                    target.Background = activeEvent;

                }
                else
                    clearTags(TagTypes.Events);
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
                clearTags(TagTypes.NonEmptyTags);
                target.Background = activeError;
            });
        }

        /// <summary>
        /// Resets all state tags back to lightGrey. Must be called from UI thread.
        /// </summary>
        private void clearTags(TagTypes type)
        {                     
            var tag = "";
            IEnumerable<Button> stateTags = StateMachine.Children.OfType<Button>();
            foreach (Button b in stateTags)
            {
                // lol. if the button has a tag, return it as a string. Otherwise tag == empty string.
                tag = (b.Tag ?? null) == null ? "" : b.Tag.ToString();

                switch (type)
                {
                    case TagTypes.NonEmptyTags:
                        if(!string.IsNullOrEmpty(tag))
                            b.Background = inactive;
                        break;
                    case TagTypes.Events:
                        if (tag.Equals("event"))
                            b.Background = inactive;
                        break;
                    case TagTypes.States:
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
