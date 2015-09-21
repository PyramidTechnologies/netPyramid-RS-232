using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyramidNETRS232_TestApp_Simple
{
    class Program
    {
        /// <summary>
        /// Dead simple demo. The error handling and extra features have been omitted for simplicity.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Enter port name in format: COMX");

            var port = Console.ReadLine();

            SingleBillValidator.Instance.Connect(port);

            Console.WriteLine("Connected on port {0}", port);

            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable) { }

            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            Console.WriteLine("Quitting...");

            SingleBillValidator.Instance.Disconnect();
        }
    }
}
