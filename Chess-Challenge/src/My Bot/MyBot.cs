// TurtleBot: Develops slowly and defensively. Waits for the opponent to run out of time.
// Control the center of the board.
// No sacrifices
// Moves are evaluated based on level of protection
// Watch for forks

using System;
using ChessChallenge.API;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public struct LUT
{
    public bool IsWhiteToMove;
    public float score;
}

public class GameInfo
{
    public string FEN { get; set; }
    public Move[] possibleMoves { get; set; }
    public Move selectedMove { get; set; }
    public float[] scores { get; set; }
}

public class MyBot : IChessBot
{
    int MAX_DEPTH = 3;
    Dictionary<ulong,LUT> hashTable;
    int currentMove;
    int randomMoveNumber;

    public MyBot()
    {
        hashTable = new Dictionary<ulong, LUT>();

        randomMoveNumber = GetRandomNumber();
    }

    int GetRandomNumber(float mean=0.5f,float std=0.2f)
    {
        Random rand = new Random();
        double u1 = 1.0 - rand.NextDouble();
        double u2 = 1.0 - rand.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        double randNormal = mean + std * randStdNormal;

        int myRandom = (int)(randNormal * 100);
        return myRandom;

    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        float[] scores = new float[moves.Length];

        if(BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 12)
        {
            MAX_DEPTH = 5;
        }

        for(int index=0; index<moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            scores[index] = EvaluateMin(board,MAX_DEPTH,float.NegativeInfinity, float.PositiveInfinity);
            board.UndoMove(moves[index]);
        }

        float maxScore = scores.Max();
        int maxIndex = scores.ToList<float>().IndexOf(maxScore);

        /***********************DEBUG ONLY************************/
        // Randomly export a board, selected move, and options.
        
        if(board.PlyCount > randomMoveNumber)
        {
            GameInfo info = new GameInfo();
            info.FEN = board.GetFenString();
            info.possibleMoves = moves;
            info.scores = scores;
            info.selectedMove = moves[maxIndex];

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize<GameInfo>(info, options);

                using (StreamWriter outputFile = new StreamWriter("/Users/kkoehler/Downloads/myBot.json", true))
                {
                    outputFile.Write(jsonString);
                }
            }
            catch
            {
                Console.WriteLine("Failed json");
            }

            randomMoveNumber = 1000;
            //randomMoveNumber = GetRandomNumber();
        }
        /*********************************************************/

        return moves[maxIndex];
    }

    float EvaluateMax(Board board, int depth, float alpha, float beta)
    {
        float maxScore = float.NegativeInfinity;
        float score;

        if (depth == 0)
        {
            return EvaluatePosition(board);
        }

        // Generate positions
        Move[] moves = board.GetLegalMoves();

        for (int index = 0; index < moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            score = EvaluateMin(board, depth-1,alpha,beta);
            board.UndoMove(moves[index]);

            maxScore = Max(score, maxScore);
            alpha = Max(alpha, maxScore);
            if (beta <= alpha)
            {
                break;
            }
        }

        return maxScore;
    }

    float EvaluateMin(Board board, int depth, float alpha, float beta)
    {
        float minScore = float.PositiveInfinity;
        float score = 0;

        if (depth == 0)
        {
            return EvaluatePosition(board);
        }

        // Generate positions
        Move[] moves = board.GetLegalMoves();

        for (int index = 0; index < moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            score = EvaluateMax(board, depth - 1,alpha,beta);
            board.UndoMove(moves[index]);

            minScore = Min(score, minScore);
            beta = Min(beta, minScore);

            if(beta <= alpha)
            {
                break;
            }
        }

        return minScore;

    }

    float EvaluatePosition(Board board)
    {
        // Do we already know the score for this board
        LUT boardScore = new LUT();

        if (!BoardLUT(board, ref boardScore))
        {
            // We don't know it yet
            boardScore.IsWhiteToMove = board.IsWhiteToMove;
            boardScore.score = 0;

            // We may need to adjust the weights of these
            // Who controls the center?
            float centerWeight = 1 + 2 / board.PlyCount;
            boardScore.score += centerWeight*CenterScore(board);

            // Decrease score for each unprotected piece
            boardScore.score -= UnprotectedPieces(board);

            // Piece score
            boardScore.score += 3*ScoreBoard(board,board.IsWhiteToMove) - ScoreBoard(board,!board.IsWhiteToMove);

            // Linked rooks
            boardScore.score += 0.5f*LinkedRooks(board);

            if (board.IsInCheck())
            {
                // Who is in check?
                if (board.SquareIsAttackedByOpponent(board.GetKingSquare(board.IsWhiteToMove)))
                {
                    boardScore.score -= 50;
                }
                else
                {
                    boardScore.score += 50;
                }
            }


            // Checkmate
            boardScore.score += (board.IsInCheckmate()) ? 100 : 0;

            // Add this to the LUT
            AddHash(board, boardScore);
        }

        return boardScore.score;
    }

    float CenterScore(Board board)
    {
        // 3 Points for pieces in the center four squares
        // 2 points for pieces in the next outer square
        // 1 point for every piece attacking a center square

        // 3 points for every piece in the center four squares
        ulong bitboard = (board.IsWhiteToMove) ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        ulong centerBits = 0x1818000000 & bitboard;
        float score = BitboardHelper.GetNumberOfSetBits(centerBits)*3;

        // 2 points for out square
        centerBits = 0x3c24243c0000 & bitboard;
        score += BitboardHelper.GetNumberOfSetBits(centerBits) * 2;

        // 1 points for every piece attacking but not in the center four squares
        Square[] centerSquares = new Square[] {new Square("d4"),
            new Square("d5"),
            new Square("e4"),
            new Square("e5")};

        // Check our attacks on center 4
        if (board.TrySkipTurn())
        {
            foreach (Square currentSquare in centerSquares)
            {
                if (board.SquareIsAttackedByOpponent(currentSquare))
                {
                    score += 1;
                }
            }
            board.UndoSkipTurn();
        }

        // -1 point for bishop, queen, and knight on the edge
        score -= BitboardHelper.GetNumberOfSetBits((board.GetPieceBitboard(PieceType.Queen, board.IsWhiteToMove) |
                    board.GetPieceBitboard(PieceType.Bishop, board.IsWhiteToMove) |
                    board.GetPieceBitboard(PieceType.Knight, board.IsWhiteToMove)) &
                    0xff818181818181ff);

        return score/22;
    }

    float UnprotectedPieces(Board board)
    {
        int score = 0;
        ulong pieces;

        // 1 for every piece that is unprotected
        pieces = (board.IsWhiteToMove) ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        while(pieces > 0)
        {
            int index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces);

            // convert bitboard index to square and check if square is attacked
            // if attacked, how much support do we have?
            if(board.SquareIsAttackedByOpponent(new Square(index)))
            {
                score += 1;
                if (board.TrySkipTurn())
                {
                    score -= 1;
                    board.UndoSkipTurn();
                }
            }
        }

        return score/16;
    }

    float LinkedRooks(Board board)
    {
        float score = 0;

        // Checks whether rooks are linked. If so, gives 5 points
        // 1) Get the rooks
        PieceList rooks = board.GetPieceList(PieceType.Rook, board.IsWhiteToMove);

        if(rooks.Count == 2)
        {
            // 2) Are they on either the same file or same row?
            bool sameRank = rooks.GetPiece(0).Square.Rank == rooks.GetPiece(1).Square.Rank;
            bool sameFile = rooks.GetPiece(0).Square.File == rooks.GetPiece(1).Square.File;

            if (sameRank || sameFile)
            {
                score += 1;
            }

        }

        return score;
    }

    int ScoreBoard(Board board,bool isWhite)
    {
        int score = 0;

        // Who has the best pieces on the board?
        // {Q=20, R=15, B=8, N=8, P=1}
        score += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Queen, isWhite)) * 20 +
                 BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Rook, isWhite)) * 15 +
                 BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Bishop, isWhite)) * 10 +
                 BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Knight, isWhite)) * 8 +
                 BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Pawn, isWhite)) * 1;

        return score/94;
    }

    void AddHash(Board board, LUT lut)
    {
        hashTable.Add(board.ZobristKey, lut);
    }

    bool BoardLUT(Board board, ref LUT lut)
    {
        if (hashTable.ContainsKey(board.ZobristKey))
        {
            lut = hashTable[board.ZobristKey];

            if(lut.IsWhiteToMove == board.IsWhiteToMove)
            {
                return true;
            }
        }

        return false;
    }

    static float Max(float a, float b)
    {
        return a > b ? a : b;
    }

    static float Min(float a, float b)
    {
        return a < b ? a : b;
    }

}