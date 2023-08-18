using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

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

			for(int index=0; index<source.Count(); ++index)
			{
				GameInfo currentGame = source[index];
				evaluate.LoadFEN(currentGame.FEN);
				ScoreStruct currentScore = evaluate.EvaluatePosition();

				source[index].centerScore = currentScore.centerScore;
				source[index].pieceScore = currentScore.pieceScore;
				source[index].rookScore = currentScore.rooksScore;
				source[index].checkmateScore = currentScore.checkmateScore;
				source[index].totalScore = currentScore.centerScore + currentScore.pieceScore + currentScore.rooksScore + currentScore.checkmateScore;
				source[index].nextTurn = (currentGame.move % 2 == 0) ? 'w' : 'b';
			}

			string jsonString = JsonSerializer.Serialize<List<GameInfo>>(source);

			using (StreamWriter outputFile = new StreamWriter("/Users/kkoehler/Downloads/test.json"))
			{
				outputFile.Write(jsonString);
			}
		}

        public static void Main(string[] args)
        {
			string jsonFile;

			if(args.Count() == 0)
			{
                Console.WriteLine("Enter a json file: ");
                jsonFile = Console.ReadLine();
			}
			else
			{
				jsonFile = args[0];
			}

			ReadFile(jsonFile);

		}

	}

	public class GameInfo
	{
		public string FEN { get; set; }
		//public List<string> tags { get; set; }
		public int totalMoves { get; set; }
		public int move { get; set; }
		public string winner { get; set; }
		public int centerScore { get; set; }
		public int pieceScore { get; set; }
		public int rookScore { get; set; }
		public int checkmateScore { get; set; }
		public int totalScore { get; set; }
		public char nextTurn { get; set; }
	}
}

