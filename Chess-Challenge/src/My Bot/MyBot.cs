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
    public int move { get; set; }
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

    int GetRandomNumber()
    {
        Random rand = new Random();
        return rand.Next(2, 20);

    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        float[] scores = new float[moves.Length];

        if(BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 12)
        {
            MAX_DEPTH = 5;
        } else if(timer.MillisecondsRemaining < 10000)
        {
            MAX_DEPTH = 3;
        }

        for(int index=0; index<moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            scores[index] = EvaluateMin(board,board.IsWhiteToMove, MAX_DEPTH,float.NegativeInfinity, float.PositiveInfinity);
            board.UndoMove(moves[index]);
        }

        float maxScore = scores.Max();
        int maxIndex = scores.ToList<float>().IndexOf(maxScore);

        /***********************DEBUG ONLY************************/
        // Randomly export a board, selected move, and options.
        
        /*if(board.PlyCount > randomMoveNumber)
        {*/
            GameInfo info = new GameInfo();
            info.FEN = board.GetFenString();
            info.possibleMoves = moves;
            info.scores = scores;
            info.selectedMove = moves[maxIndex];
            info.move = board.PlyCount;

            try
            {
                //var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize<GameInfo>(info) + ", ";

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
        //}
        /*********************************************************/

        return moves[maxIndex];
    }

    float EvaluateMax(Board board, bool playerIsWhite, int depth, float alpha, float beta)
    {
        float maxScore = float.NegativeInfinity;
        float score;

        if (depth == 0)
        {
            return EvaluatePosition(board, playerIsWhite);
        }

        // Generate positions
        Move[] moves = board.GetLegalMoves();

        for (int index = 0; index < moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            score = EvaluateMin(board, playerIsWhite, depth-1,alpha,beta);
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

    float EvaluateMin(Board board, bool playerIsWhite, int depth, float alpha, float beta)
    {
        float minScore = float.PositiveInfinity;
        float score = 0;

        if (depth == 0)
        {
            return EvaluatePosition(board, playerIsWhite);
        }

        // Generate positions
        Move[] moves = board.GetLegalMoves();

        for (int index = 0; index < moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            score = EvaluateMax(board, playerIsWhite, depth - 1,alpha,beta);
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

    float EvaluatePosition(Board board, bool playerIsWhite)
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
            float centerWeight = 10;
            boardScore.score += centerWeight*CenterScore(board,playerIsWhite);

            // Decrease score for each unprotected piece
            boardScore.score -= UnprotectedPieces(board,playerIsWhite);

            // Piece score
            boardScore.score += 10*(ScoreBoard(board,playerIsWhite) - ScoreBoard(board,!playerIsWhite));

            // Linked rooks
            boardScore.score += 0.5f*LinkedRooks(board,playerIsWhite);

            /*if (board.IsInCheck())
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
            }*/


            // Checkmate
            boardScore.score += (board.IsInCheckmate()) ? 100 : 0;

            // Add this to the LUT
            AddHash(board, boardScore);
        }

        return boardScore.score;
    }

    float CenterScore(Board board, bool playerIsWhite)
    {
        // 3 Points for pieces in the center four squares
        // 2 points for pieces in the next outer square
        // 1 point for every piece attacking a center square

        // 3 points for every piece in the center four squares
        ulong bitboard = (playerIsWhite) ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
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
        score -= BitboardHelper.GetNumberOfSetBits((board.GetPieceBitboard(PieceType.Queen, playerIsWhite) |
                    board.GetPieceBitboard(PieceType.Bishop, playerIsWhite) |
                    board.GetPieceBitboard(PieceType.Knight, playerIsWhite)) &
                    0xff818181818181ff);

        return score/22;
    }

    float UnprotectedPieces(Board board, bool playerIsWhite)
    {
        int score = 0;
        ulong pieces;

        // 1 for every piece that is unprotected
        pieces = (playerIsWhite) ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
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

        return score;
    }

    float LinkedRooks(Board board, bool playerIsWhite)
    {
        float score = 0;

        // Checks whether rooks are linked. If so, gives 5 points
        // 1) Get the rooks
        PieceList rooks = board.GetPieceList(PieceType.Rook, playerIsWhite);

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