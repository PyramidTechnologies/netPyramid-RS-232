using PyramidNETRS232;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PauseResumeCLI
{
    /// <summary>
    /// Simplified state machine. You could also key off of
    /// the validator.PreviousState property for the same effect.
    /// </summary>
    enum InternalState
    {
        /// <summary>
        /// Acceptor is not in escrow or stacking
        /// </summary>
        Idle, 

        /// <summary>
        /// Acceptor is awaiting master command to acceptor or return (reject)
        /// </summary>
        Escrow, 

        /// <summary>
        /// Escrow command was stack, awaiting credit message
        /// </summary>
        AwaitingCredit
    }

    class Program
    {
        /// <summary>
        /// Application demonstrates how to use the pause/resume api
        /// Tested against firmware Apex74USA111.PTI
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("Enter port name in format: COMX");

            var port = Console.ReadLine();
            PyramidAcceptor validator = null;
            

            try
            {
                // Toggle this to play with escrow vs. non-escrow
                var escrowMode = true;
                var cfg = new RS232Config(port, escrowMode);
                validator = new PyramidAcceptor(cfg);
                
                // We need the escrow and credit events for this example
                // Note that OnEscrow will not be called if you set escrowMode
                // to false on line 49.
                validator.OnEscrow +=validator_OnEscrow;
                validator.OnCredit +=validator_OnCredit;

                // Connect and await 1st response from slave
                // Consider adding a timeout here
                validator.Connect();
                while (!validator.IsRunning) { Thread.Sleep(10); }


                // Console IO helpers - only used for this console app
                ConsoleKeyInfo cki = new ConsoleKeyInfo();
                Console.WriteLine("Connected on port {0}", port);


                var helpText = "Commands\n\tq: Quit\n\tp: Pause\n\tu: Unpause\n\n\th: Help";
                Console.WriteLine(helpText);


                // Main message loop Exit when the validator has shutdown comms
                while (validator.IsRunning)
                {
                    // Decode the keypress if any are available
                    if (Console.KeyAvailable)
                    {
                        cki = Console.ReadKey(true);

                        switch (cki.KeyChar)
                        {
                            // Quit
                            case 'q':
                                Console.WriteLine("Shutting down...");
                                validator.Close();
                                break;

                            // Pause:
                            case 'p':
                                if (AppState != InternalState.AwaitingCredit)
                                {
                                    Console.WriteLine("Acceptance Paused. 'u' to unpause...");
                                    validator.PauseAcceptance();
                                }
                                else
                                {
                                    Console.WriteLine("Cannot pause yet, finishing stack operation...");
                                }
                                break;

                            // Unpause
                            case 'u':
                                Console.WriteLine("Acceptance Resumed. 'p' to unpause...");
                                validator.ResmeAcceptance();
                                break;

                            // Show command chars
                            case 'h':
                                Console.WriteLine(helpText);
                                break;
                        }
                    }

                    // If Escrowed, prompt for stack or reject
                    else if (AppState == InternalState.Escrow)
                    {
                        Console.WriteLine("Stack or Reject? [s, r]");
                        cki = Console.ReadKey(true);

                        switch (cki.KeyChar)
                        {
                            case 's':
                                // Once we stack, set our state so we don't get stuck in this escroww loop
                                validator.Stack();
                                AppState = InternalState.AwaitingCredit;
                                Console.WriteLine("Stacking, please wait...");
                                break;


                            case 'r':
                                // For this example we do not care about what reject means other than
                                // "don't accept the money". Assume we're idle immediately. In a real app
                                // you would want to monitor the events for a bill returned event so you
                                // know that the bill made it out.
                                validator.Reject();
                                AppState = InternalState.Idle;
                                Console.WriteLine("Returning bill...");
                                break;
                        }

                    }

                    else
                    {
                        // Burn some time so other works can get done
                        Thread.Sleep(10);
                    }

                }



                Console.WriteLine("Bye!");

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            finally
            {
                if (validator != null)
                {
                    validator.Dispose();
                }
            }

        }

        private static InternalState AppState { get; set; }

        private static void validator_OnEscrow(object sender, EscrowArgs e)
        {
            Console.WriteLine("Bill {0} is in escrow", e.Index);
            AppState = InternalState.Escrow;
        }

        private static void validator_OnCredit(object sender, CreditArgs e)
        {
            Console.WriteLine("Bill {0} has credited", e.Index);
            AppState = InternalState.Idle;
        }
    }
}
