using System;
using System.Collections.Generic;

namespace BoardAnalysis.Application
{
	public class Program
	{
        public static void Main()
        {
			string FEN;
			ScoreStruct score;
			List<string> inputFENs = new List<string>();
			

			do
			{
				Console.WriteLine("Input a FEN: ");
				FEN = Console.ReadLine();
				if(FEN != null)
				{
                    inputFENs.Add(FEN);
                }
			} while (FEN != null && !FEN.Contains("quit"));

			// Process the FENs and output to a JSON file
			foreach(string currentFEN in inputFENs)
			{

			}

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

