using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TimeWaster.XordleBoard;

namespace TimeWaster.Xordle
{
    public static class Showdown
    {
        public static void Fight(int matches)
        {
            var words = new List<string>(File.ReadAllLines("Octordle.wordlist"));

            var stats = new List<int>();
            var unsolvedWords = new List<string>(); // Words that took too many guesses

            for (int i = 0; i < matches; i++)
            {
                var unknown = new UnknownBoard(0, words);
                var known = new KnownBoard(unknown.GetNextGuess());

                unknown.AddGuess("ROAST"); // 399 & 371 & 399. Really good starter
                unknown.AddGuess("FIELD");

                while (true)
                {
                    var guess = unknown.GetNextGuess();
                    unknown.AddGuess(guess);
                    var cpa = known.AddGuess(guess);
                    unknown.UpdateResult(guess, cpa);

                    if (unknown.IsSolved)
                    {
                        stats.Add(unknown.GuessCount);

                        if (unknown.GuessCount >= 6)
                        {
                            unsolvedWords.Add(known.Solution);
                            break;
                        }
                        
                        break;
                    }

                    if (unknown.IsBroken)
                    {
                        Console.Write($"Broken board ({known.Solution}).  Guesses: ");
                        Console.WriteLine($"\t{string.Join("  ", unknown.GetGuesses())}");
                        stats.Add(-1);
                        break;
                    }
                }

                // Output a dot every
                if (i % 100 == 0) Console.Write(".");
            }

            Console.WriteLine();
            var group = unsolvedWords.GroupBy(w => w);
            Console.WriteLine($"Broken words: ${string.Join("  ", group.OrderBy(g => g.Count()).Select(g => $"{g.Key}({g.Count()})"))}");

            Console.WriteLine();
            foreach (var grouping in stats.GroupBy(s => s).OrderBy(g => g.Key))
            {
                Console.WriteLine($"{grouping.Key} - {grouping.Count()} - {Math.Round((decimal)grouping.Count()/matches*100, 1)}%");
            }

            Console.WriteLine($"{unsolvedWords.Count()} words unsolved");
        }
    }
}