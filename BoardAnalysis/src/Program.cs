using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BoardAnalysis.Application
{
	public class Program
	{
		public static void ReadFile(string filename)
		{
			List<GameInfo> source = new List<GameInfo>();

			using (StreamReader r = new StreamReader(filename))
			{
				string json = r.ReadToEnd();
				source = JsonSerializer.Deserialize<List<GameInfo>>(json);
			}

			Evaluate evaluate = new Evaluate();

			foreach(GameInfo currentGame in source)
			{
				evaluate.LoadFEN(currentGame.FEN);
				ScoreStruct currentScore = evaluate.EvaluatePosition();
				Console.WriteLine(currentScore);
			}
		}

        public static void Main()
        {
			Console.WriteLine("Enter a json file: ");
			string jsonFile = Console.ReadLine();

			ReadFile(jsonFile);

			/*string FEN;
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
			Console.WriteLine("Checkmate Score: " + score.checkmateScore);*/
		}

	}

	public class GameInfo
	{
		public string FEN { get; set; }
		public int totalMoves { get; set; }
		public int currentMove { get; set; }
		public string winner { get; set; }
	}
}

