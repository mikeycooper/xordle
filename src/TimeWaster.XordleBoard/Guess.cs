using System;
using System.Linq;

namespace TimeWaster.XordleBoard
{
    public class Guess
    {
        public string Word { get; }
        public string Result { get; private set; }

        public Guess(string word, string result)
        {
            Word = word;
            Result = result;
        }

        public void SetResult(string result)
        {
            if (result.Any(c => c != 'C' && c != 'P' && c != 'A'))
                throw new Exception($"Result {result} contains an invalid CPA identifier");

            Result = result;
        }

        public bool IsSolved => Result == "CCCCC";
    }
}