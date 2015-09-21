using PyramidNETRS232;
using System;

namespace PyramidNETRS232_TestApp_Simple
{
    public class SingleBillValidator
    {
        private static volatile SingleBillValidator instance;
        private static object syncRoot = new Object();
        
        private PyramidAcceptor validator { get; set; }
        
        public RS232Config Config { get; set; }

        public static SingleBillValidator Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new SingleBillValidator();
                        }
                    }
                }

                return instance;
            }
        }

        private SingleBillValidator()
        {

        }
        
        public void Connect(string port)
        {
            Config = new RS232Config(port, false);
            validator = new PyramidAcceptor(Config);

            validator.OnCredit += validator_OnCredit;

            validator.Connect();
        }

        void validator_OnCredit(object sender, CreditArgs e)
        {
            Console.WriteLine("Credited bill#: {0}", e.Index);
        }

        public void Disconnect()
        {
            if (validator != null)
                validator.Close();
        }
    }
}
