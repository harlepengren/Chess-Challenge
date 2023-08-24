// TurtleBot: Develops slowly and defensively. It focuses on:
// - Controlling the center of the board.
// - Keeping pieces relatively equal
// - Ensuring pieces are protected

using System;
using ChessChallenge.API;
using System.Linq;
using System.Collections.Generic;

public struct LUT
{
    public bool IsWhiteToMove;
    public float score;
}

public class MyBot : IChessBot
{
    int MAX_DEPTH = 3;
    Dictionary<ulong,LUT> hashTable;

    public MyBot()
    {
        hashTable = new Dictionary<ulong, LUT>();
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        float[] scores = new float[moves.Length];

        for(int index=0; index<moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            scores[index] = EvaluateMin(board,board.IsWhiteToMove, MAX_DEPTH,float.NegativeInfinity, float.PositiveInfinity);
            board.UndoMove(moves[index]);
        }

        return moves[scores.ToList<float>().IndexOf(scores.Max())];
    }

    // Maximize the position
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
            float centerWeight = (board.PlyCount < 30) ? 5 : 2.0f;
            boardScore.score += centerWeight*CenterScore(board,playerIsWhite);

            // Decrease score for each unprotected piece
            boardScore.score -= UnprotectedPieces(board,playerIsWhite);

            // Piece score
            boardScore.score += 20*(ScoreBoard(board,board.IsWhiteToMove) - ScoreBoard(board,!board.IsWhiteToMove));

            // Check castling - Not sure how to keep from making a move that removes the ability to castle, but
            // if the move itself is a castle, that is a good thing.
            boardScore.score += (board.HasKingsideCastleRight(board.IsWhiteToMove) || board.HasQueensideCastleRight(board.IsWhiteToMove)) ? 3 : 0;

            // Find any key pieces on the edge.
            boardScore.score -= 1.5f*EdgeScore(board, playerIsWhite);

            // Check & Checkmate
            boardScore.score += (board.IsInCheck()) ? 10 : 0;
            boardScore.score += (board.IsInCheckmate()) ? 200 : 0;

            // Add this to the LUT
            hashTable.Add(board.ZobristKey, boardScore);
            //AddHash(board, boardScore);
        }

        return boardScore.score;
    }

    // Evaluates control of the center of the board:
    //   Pieces physically in the center 4 squares
    //   Pieces in the surrounding 12
    //   Pieces attacking the center 4
    int CenterScore(Board board, bool playerIsWhite)
    {
        // 4 points for every piece in the center four squares
        ulong bitboard = (playerIsWhite) ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        //ulong centerBits = 0x1818000000 & bitboard;
        int score = BitboardHelper.GetNumberOfSetBits(0x1818000000 & bitboard) *4;

        // 2 points for out square
        //centerBits = 0x3c24243c0000 & bitboard;
        score += BitboardHelper.GetNumberOfSetBits(0x3c24243c0000 & bitboard) * 2;

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

        return score;
    }

    // Number of Queens, bishops, and knights that are on the edge of the board
    int EdgeScore(Board board, bool playerIsWhite)
    {
        return BitboardHelper.GetNumberOfSetBits((board.GetPieceBitboard(PieceType.Queen, playerIsWhite) |
                    board.GetPieceBitboard(PieceType.Bishop, playerIsWhite) |
                    board.GetPieceBitboard(PieceType.Knight, playerIsWhite)) &
                    0xff818181818181ff);
    }

    // Number of pieces that are currently attacked, but are unprotected
    int UnprotectedPieces(Board board, bool playerIsWhite)
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
            Square currentSquare = new Square(index);
            if (board.SquareIsAttackedByOpponent(new Square(index)))
            {
                score += 1;
                if (board.TrySkipTurn())
                {
                    if(board.SquareIsAttackedByOpponent(new Square(index)))
                    {
                        score -= 1;
                    }
                    board.UndoSkipTurn();
                }
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

        return score;
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