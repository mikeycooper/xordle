using System.Collections.Generic;

namespace TimeWaster.XordleBoard
{
    public class UnknownBoardSaveDto
    {
        public int Id { get; set; }
        public List<Guess> Guesses { get; set; }
    }
}