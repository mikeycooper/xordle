using System.Collections.Generic;
using System.Linq;

namespace TimeWaster.XordleBoard
{
    public class KnownBoard : Board
    {
        public string Solution { get; set; }

        /// <summary>
        /// Constructor for a board with a known solution
        /// </summary>
        public KnownBoard(string solution)
        {
            Solution = solution;
        }

        protected override string EvaluateGuess(string guess)
        {
            guess = guess.ToLower();

            var results = ToCharacterResults(guess);
            var possiblePresents = new List<char>();
            
            // First check for all letters in the guess that are in the correct spots
            foreach (var result in results)
            {
                if (result.Letter == Solution[result.Position])
                    result.Result = 'C';
                else
                    possiblePresents.Add(Solution[result.Position]);
            }

            // Now check for any present characters in what's left
            foreach (var result in results.Where(r => r.Result == 'A'))
            {
                var index = possiblePresents.IndexOf(result.Letter);
                if (index >= 0)
                {
                    result.Result = 'P';
                    possiblePresents.RemoveAt(index);
                }
            }

            return new string(results.OrderBy(r => r.Position).Select(r => r.Result).ToArray());
        }

        private static List<CharacterResult> ToCharacterResults(string guess)
        {
            var results = new List<CharacterResult>();
            for (int i = 0; i < guess.Length; i++)
            {
                results.Add(new CharacterResult { Position = i, Letter = guess[i], Result = 'A' });
            }
            return results;
        }

        private class CharacterResult
        {
            public int Position { get; set; }
            public char Letter { get; set; }
            public char Result { get; set; }
        }
    }
}