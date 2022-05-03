using System;
using System.Diagnostics;

namespace TimeWaster.Xordle
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();

            var sw = Stopwatch.StartNew();
            //Showdown.Fight(10000);
            XordleGame.Xordle(args);
            sw.Stop();
            Console.WriteLine($"Completed in {sw.ElapsedMilliseconds/1000}s");
        }
    }
}
