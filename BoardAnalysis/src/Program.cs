using System;

namespace BoardAnalysis.Application
{
	public class Program
	{
        public static void Main()
        {
			string FEN;
			ScoreStruct score;

			Console.WriteLine("Input a FEN: ");
			FEN = Console.ReadLine();

			// Input a FEN and output the scoring factors
			Evaluate myEval = new Evaluate(FEN);

			score = myEval.EvaluatePosition();

			Console.WriteLine("Center Score: " + score.centerScore);
			Console.WriteLine("Piece Score: " + score.pieceScore);
			Console.WriteLine("Rook Score: " + score.rooksScore);
			Console.WriteLine("Checkmate Score: " + score.checkmateScore);
		}

	}
}

