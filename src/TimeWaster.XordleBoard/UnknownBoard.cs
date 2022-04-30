using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TimeWaster.XordleBoard
{
    public class UnknownBoard : Board
    {
        private List<string> remainingWords;
        private List<string> originalWords;
        private Random rand = new Random();
        public int Id { get; set; }
        public int GuessCount => Guesses.Count;
        public int RemainingWordCount => remainingWords.Count;
        public bool HasPendingGuesses => Guesses.Any(g => string.IsNullOrWhiteSpace(g.Result));
        public bool IsBroken => !IsSolved && remainingWords.Count == 0;
        public override bool IsTerminal => base.IsTerminal || this.IsBroken;

        public UnknownBoard(int id, IEnumerable<string> words) 
        {
            this.Id = id;
            this.remainingWords = new List<string>(words);
            this.originalWords = new List<string>(words);
        }

        public IEnumerable<string> GetGuesses() => Guesses.Select(g => g.Word);

        public IEnumerable<Guess> GetEvaluatedGuesses() => Guesses.Where(g => !string.IsNullOrWhiteSpace(g.Result));

        public void ClearPendingGuesses() => Guesses.RemoveAll(g => string.IsNullOrWhiteSpace(g.Result));

        public void UpdateResult(string guess, string cpa)
        {
            guess = guess.ToLower();
            cpa = cpa.ToUpper();

            bool guessAdded = false;
            if (!Guesses.Any(g => g.Word == guess))
            {
                guessAdded = true;
                Guesses.Add(new Guess(guess, string.Empty));
            }

            Guesses.First(g => g.Word == guess).SetResult(cpa);

            if (cpa == "CCCCC") return;

            // Remove all words that don't match the correct/absent regex pattern
            var correctRegex = new Regex(GetRegexPattern(guess, cpa));

            var testWords = new List<string>(remainingWords);
            testWords.RemoveAll(w => !correctRegex.Match(w).Success);
            RemoveWordsMissingPresent(testWords, guess, cpa);

            if (testWords.Count == 0)
            {
                if (guessAdded) Guesses.Remove(Guesses.First(g => g.Word == guess));
                else Guesses.First(g => g.Word == guess).SetResult(string.Empty);
                throw new BrokenBoardException();
            }

            remainingWords = testWords;
        }

        public string GetNextGuess()
        {
            // Do we have any guesses queued up waiting for evaluation?
            var guess = Guesses.FirstOrDefault(g => string.IsNullOrWhiteSpace(g.Result));
            if (guess != null)
            {
                return guess.Word;
            }

            // If we've used up all the queued guesses, just return a random
            // word from the remaining dictionary. First make sure the board
            // isn't broken
            if (IsBroken) return "";
            var randIndex = rand.Next(remainingWords.Count);

            return remainingWords[randIndex];
        }

        public IEnumerable<string> GetWords() => remainingWords.Select(w => w);

        public void ResetBoard()
        {
            foreach (var guess in GetEvaluatedGuesses()) guess.SetResult("");
            remainingWords = originalWords;
        }

        private void RemoveWordsMissingPresent(List<string> wordList, string guess, string cpa)
        {
            var presentCharacters = GetPresentCharacters(guess, cpa);
            if (presentCharacters.Length > 0)
            {
                var wordsToRemove = new List<string>();
                foreach (var word in wordList)
                {
                    var stripped = StripWord(word, cpa);
                    foreach (var presentCharacter in presentCharacters)
                    {
                        if (!stripped.Contains(presentCharacter))
                        {
                            wordsToRemove.Add(word);
                            break;
                        }
                    }
                }
                wordList.RemoveAll(w => wordsToRemove.Contains(w));
            }
        }

        private string StripWord(string word, string cpa)
        {
            cpa = cpa.ToUpper();

            string stripped = "";
            for (int i = 0; i < cpa.Length; i++)
            {
                if (cpa[i] == 'C') continue;
                stripped += word[i];
            }

            return stripped;
        }

        private string GetRegexPattern(string guess, string result)
        {
            if (guess.Length != 5 || result.Length != 5) throw new ArgumentException("Guess and result must both be 5 characters");

            string presentCharacters = "";
            for (int i = 0; i < 5; i++)
            {
                presentCharacters += result[i] == 'P' ? guess[i].ToString() : "";
            }
            string absentCharacters = "";
            for (int i = 0; i < 5; i++)
            {
                absentCharacters += result[i] == 'A' && !presentCharacters.Contains(guess[i]) ? guess[i].ToString() : "";
            }

            string regex = "";
            for (int i = 0; i < 5; i++)
            {
                switch(result[i])
                {
                    case 'C':
                        regex += guess[i];
                        break;
                    case 'A':
                        if (absentCharacters.Length > 0) regex += $"[^{absentCharacters}]";
                        else regex += ".";
                        break;
                    case 'P':
                        regex += $"[^{guess[i]}{absentCharacters}]";
                        break;
                    default:
                        regex += ".";
                        break;
                }
            }

            return regex;
        }

        private string GetPresentCharacters(string guess, string result)
        {
            if (guess.Length != 5 || result.Length != 5) throw new ArgumentException("Guess and result must both be 5 characters");

            string presentCharacters = "";
            for (int i = 0; i < 5; i++)
            {
                presentCharacters += result[i] == 'P' ? guess[i].ToString() : "";
            }

            return presentCharacters;
        }
    }
}