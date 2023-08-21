using System;

namespace BoardAnalysis.Application
{
    public class GameInfo
    {
        public string FEN { get; set; }
        //public List<string> tags { get; set; }
        public int totalMoves { get; set; }
        public int move { get; set; }
        public string winner { get; set; }
        public float centerScore { get; set; }
        public float centerAttackScore { get; set; }
        public float oppAttackScore { get; set; }
        public float slidingEdgeScore { get; set; }
        public float oppCenterScore { get; set; }
        public float pieceScore { get; set; }
        public float oppPieceScore { get; set; }
        public float rookScore { get; set; }
        public float checkmateScore { get; set; }
        public float totalScore { get; set; }
        public char nextTurn { get; set; }
        public float unprotectedScore { get; set; }
    }
}

