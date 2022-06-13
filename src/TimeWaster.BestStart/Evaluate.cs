using System.Reflection;
using System.Text.Json;
using TimeWaster.XordleBoard;

namespace TimeWaster.BestStart;

public static class Evaluate
{
    public static void EvaluateDictionary()
    {
        var words = GetWordList();
        Dictionary<string, int> results = new(words.Count);

        foreach (var word in words.Select(w => w.ToUpper()))
        {
            var solves = EvaluateWord(word, words);
            results.Add(word, solves);
            Console.WriteLine($"{word} solves {solves} words");
        }

        File.WriteAllText(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "BestStart.json"),
            JsonSerializer.Serialize(results.OrderByDescending(r => r.Value)));
    }

    private static int EvaluateWord(string guess, List<string> words)
    {
        int solves = 0;

        foreach (var word in words)
        {
            var unknown = new UnknownBoard(0, words);
            var known = new KnownBoard(word);

            unknown.AddGuess(guess);

            var cpa = known.AddGuess(guess);
            unknown.UpdateResult(guess, cpa);

            if (unknown.IsSolved || unknown.RemainingWordCount == 1)
            {
                solves++;
            }
        }

        return solves;
    }

    private static List<string> GetWordList() => new(
    File.ReadAllLines(
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
            "Octordle.wordlist")));
}
