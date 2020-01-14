using System;

namespace DigaCoreHttpServer
{
    class Program
    {
        private static void Main(string[] args)
        {
            string currentPath = Environment.CurrentDirectory;
            Console.WriteLine("path:" + currentPath);
            using (SimpleHttpServer server = new SimpleHttpServer(currentPath,5001))
            {
                Console.WriteLine("Press ctl-Q to exit!");
                Console.CancelKeyPress+=OnCancelKeyPress;
                char k = 'X';
                while (k != 'q')
                {
                    var key = Console.ReadKey(false);
                    if (key.Key == ConsoleKey.Q && key.Modifiers == ConsoleModifiers.Control)
                    {
                        k = 'q';
                    }
                    if(k!='q')
                        Console.WriteLine("Press ctl-Q to exit!");
                    
                }

                server.Stop();
                
            }
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if(e.SpecialKey == ConsoleSpecialKey.ControlBreak)
                e.Cancel = true;
        }
    }
}
