using System.Collections.Generic;
using System.ComponentModel;

namespace PyramidNETRS232_TestApp
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
        private int total = 0;

        private static Dictionary<int, int> currencyMap = new Dictionary<int, int>();
        private Dictionary<int, long> cashbox = new Dictionary<int, long>();
        #endregion

        // Configure our currency map
        static MainWindow()
        {
            currencyMap.Add(1, 1);
            currencyMap.Add(2, 2);
            currencyMap.Add(3, 5);
            currencyMap.Add(4, 10);
            currencyMap.Add(5, 20);
            currencyMap.Add(6, 50);
            currencyMap.Add(7, 100);
        }

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

        public int Total
        {
            get { return total; }
            set
            {
                total = value;
                NotifyPropertyChanged("Total");
            }
        }
        #endregion

        /// <summary>
        /// Adds credit to the specified slot, returns the denomination that was credited
        /// </summary>
        /// <param name="denom"></param>
        /// <returns></returns>
        internal int AddCredit(int denom)
        {
            switch(denom)
            {
                case 1:
                    Bill1++;
                    break;
                case 2:
                    Bill2++;
                    break;
                case 3:
                    Bill3++;
                    break;
                case 4:
                    Bill4++;
                    break;
                case 5:
                    Bill5++;
                    break;
                case 6:
                    Bill6++;
                    break;
                case 7:
                    Bill7++;
                    break;

                default:
                    // Illegal value
                    return 0;
            }

            // Return translated value and increment bill bank total
            var val = currencyMap[denom];
            Total += val;
            return val;
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
