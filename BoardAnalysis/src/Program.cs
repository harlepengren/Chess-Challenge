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
				GameInfo currentScore = evaluate.EvaluatePosition();

				source[index].centerScore = currentScore.centerScore;
				source[index].oppCenterScore = currentScore.oppCenterScore;
				source[index].centerAttackScore = currentScore.centerAttackScore;
				source[index].oppAttackScore = currentScore.oppAttackScore;
                source[index].slidingEdgeScore = currentScore.slidingEdgeScore;
                source[index].pieceScore = currentScore.pieceScore;
				source[index].oppPieceScore = currentScore.oppPieceScore;
                source[index].rookScore = currentScore.rookScore;
				source[index].unprotectedScore = currentScore.unprotectedScore;
				source[index].checkmateScore = currentScore.checkmateScore;
				source[index].totalScore = currentScore.centerScore + currentScore.pieceScore + currentScore.rookScore + currentScore.checkmateScore;
				source[index].nextTurn = currentScore.nextTurn;
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

	
}

