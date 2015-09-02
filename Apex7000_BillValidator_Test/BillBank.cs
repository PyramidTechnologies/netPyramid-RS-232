using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Apex7000_BillValidator_Test
{
    partial class MainWindow : INotifyPropertyChanged
    {
        #region Fields
        private int bill1 = 0;
        private int bill2 = 0;
        private int bill3 = 0;
        private int bill4 = 0;
        private int bill5 = 0;
        private int bill6 = 0;
        private int bill7 = 0;
        #endregion

        #region Properties
        public int Bill1
        {
            get { return bill1; }
            set
            {
                bill1 = value;
                NotifyPropertyChanged("Bill1");
            }
        }

        public int Bill2
        {
            get { return bill2; }
            set
            {
                bill2 = value;
                NotifyPropertyChanged("Bill2");
            }
        }

        public int Bill3
        {
            get { return bill3; }
            set
            {
                bill3 = value;
                NotifyPropertyChanged("Bill3");
            }
        }

        public int Bill4
        {
            get { return bill4; }
            set
            {
                bill4 = value;
                NotifyPropertyChanged("Bill4");
            }
        }

        public int Bill5
        {
            get { return bill5; }
            set
            {
                bill5 = value;
                NotifyPropertyChanged("Bill5");
            }
        }

        public int Bill6
        {
            get { return bill6; }
            set
            {
                bill6 = value;
                NotifyPropertyChanged("Bill6");
            }
        }

        public int Bill7
        {
            get { return bill7; }
            set
            {
                bill7 = value;
                NotifyPropertyChanged("Bill7");
            }
        }
        #endregion

        internal int AddCredit(int denom)
        {
            switch(denom)
            {
                case 1:
                    return Bill1++;
                case 2:
                    return Bill2++;
                case 3:
                    return Bill3++;
                case 4:
                    return Bill4++;
                case 5:
                    return Bill5++;
                case 6:
                    return Bill6++;
                case 7:
                    return Bill7++;

                default:
                    // Illegal value
                    return 0;
            }
        }


        #region Private Helpers

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;


        #endregion

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
