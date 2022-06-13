using System.Reflection;
using TimeWaster.XordleBoard;

namespace TimeWaster.XordleSolver;

public static class Showdown
{
    public static void Fight(int matches, string[] args)
    {
        var words = GetWordList();

        var stats = new List<int>();
        var unsolvedWords = new List<string>(); // Words that took too many guesses

        List<string> startingWords = new();
        if (args.Length > 0)
        {
            foreach (var arg in args)
            {
                startingWords.Add(arg.ToUpper());
            }
        }
        else
        {
            startingWords.Add("ROAST"); // 399 & 371 & 399. Really good starter
            startingWords.Add("FIELD");
        }

        for (int i = 0; i < matches; i++)
        {
            var unknown = new UnknownBoard(0, words);
            var known = new KnownBoard(unknown.GetNextGuess());

            foreach (var startingWord in startingWords)
            {
                unknown.AddGuess(startingWord.ToUpper());
            }

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

        Console.WriteLine(string.Join("  ", startingWords));

        Console.WriteLine();
        foreach (var grouping in stats.GroupBy(s => s).OrderBy(g => g.Key))
        {
            Console.WriteLine($"{grouping.Key} - {grouping.Count()} - {Math.Round((decimal)grouping.Count()/matches*100, 1)}%");
        }

        Console.WriteLine($"{unsolvedWords.Count} words unsolved");
    }

    private static List<string> GetWordList() => new(
    File.ReadAllLines(
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
            "Octordle.wordlist")));
}
