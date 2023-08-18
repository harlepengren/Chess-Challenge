// TurtleBot: Develops slowly and defensively. Waits for the opponent to run out of time.
// Control the center of the board.
// No sacrifices
// Moves are evaluated based on level of protection
// Watch for forks

using System;
using ChessChallenge.API;
using System.Linq;
using System.Collections.Generic;

public struct LUT
{
    public bool IsWhiteToMove;
    public int score;
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
        int[] scores = new int[moves.Length];

        // Count the pieces on the board, if less than 15 increase the depth
        ulong numPieces = 0;
        ulong bitBoard = board.AllPiecesBitboard;
        while (bitBoard != 0)
        {
            numPieces += bitBoard & 1;
            bitBoard >>= 1;
        }

        if(numPieces < 10)
        {
            MAX_DEPTH = 5;
        }

        for(int index=0; index<moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            scores[index] = EvaluateMin(board,MAX_DEPTH,int.MinValue, int.MaxValue);
            board.UndoMove(moves[index]);
        }

        int maxScore = scores.Max();
        int maxIndex = scores.ToList<int>().IndexOf(maxScore);

        return moves[maxIndex];
    }

    int EvaluateMax(Board board, int depth, int alpha, int beta)
    {
        int maxScore = int.MinValue;
        int score;

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

    int EvaluateMin(Board board, int depth, int alpha, int beta)
    {
        int minScore = int.MaxValue;
        int score = 0;

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

    int EvaluatePosition(Board board)
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

            int centerWeight;
            if(board.PlyCount < 40)
            {
                centerWeight = 2;
            } else
            {
                centerWeight = 1;
            }
            boardScore.score += centerWeight * CenterScore(board);

            // Decrease score for each unprotected piece
            boardScore.score -= UnprotectedPieces();

            // Piece score
            boardScore.score += 2 * ScoreBoard(board);

            // Linked rooks
            boardScore.score += LinkedRooks(board);

            // Checkmate
            boardScore.score += (board.IsInCheckmate()) ? 100 : 0;

            // Add this to the LUT
            AddHash(board, boardScore);
        }

        return boardScore.score;
    }

    int CenterScore(Board board)
    {
        int score = 0;
        int whitePieces = 0;
        int blackPieces = 0;
        Piece[] centerPieces = new Piece[] {board.GetPiece(new Square("d4")),
                board.GetPiece(new Square("d5")),
                board.GetPiece(new Square("e4")),
                board.GetPiece(new Square("e5"))};

        // 3 points for every piece in the center four squares
        foreach (Piece currentPiece in centerPieces)
        {
            if(currentPiece.PieceType != PieceType.None)
            {        
                if (currentPiece.IsWhite)
                {
                    whitePieces++;
                }
                else
                {
                    blackPieces++;
                }   
            }
        }

        if (board.IsWhiteToMove)
        {
            score = (whitePieces - blackPieces) * 3;
        }
        else
        {
            score = (blackPieces - whitePieces) * 3;
        }

        // 2 points for every piece attacking but not in the center four squares
        Square[] centerSquares = new Square[] {new Square("d4"),
            new Square("d5"),
            new Square("e4"),
            new Square("e5")};

        foreach(Square currentSquare in centerSquares)
        {
            if (board.SquareIsAttackedByOpponent(currentSquare))
            {
                score -= 1;
            }
        }

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
        PieceList[] pieceList = board.GetAllPieceLists();
        PieceType[] target = new PieceType[] { PieceType.Queen, PieceType.Bishop, PieceType.Knight };
        foreach(PieceList currentList in pieceList)
        {
            if (target.Contains<PieceType>(currentList.TypeOfPieceInList))
            {
                foreach (Piece currentPiece in currentList)
                {
                    if (currentPiece.Square.File == 0 || currentPiece.Square.File == 7)
                    {
                        score -= 1;
                    }
                }
            }
        }

        return score;
    }

    /*int UnprotectedPieces()
    {
        int score = 0;

        // 1 for every piece that is unprotected
        // foreach of our pieces
        // Get the position
        // If 0 of our pieces are attacking that square, subtract 1

        return score;
    }*/

    int LinkedRooks(Board board)
    {
        int score = 0;

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

    int ScoreBoard(Board board)
    {
        int score = 0;
        int playerBonus = 1;

        // Who has the best pieces on the board?
        // {Q=20, R=15, N=10, B=8, P=1}
        PieceList[] pieces = board.GetAllPieceLists();
        foreach (PieceList currentPieces in pieces)
        {
            playerBonus = (board.IsWhiteToMove == currentPieces.IsWhitePieceList) ? 1 : -1;
            score += ScorePiece(currentPieces.TypeOfPieceInList, currentPieces.Count) * playerBonus;
        }

        // Positive score means we have the best pieces, negative means they do

        return score;
    }

    int ScorePiece(PieceType piece, int count)
    {
        int score = 0;

        switch (piece)
        {
            case PieceType.Queen:
                score = 20;
                break;
            case PieceType.Rook:
                score = 15;
                break;
            case PieceType.Bishop:
                score = 8;
                break;
            case PieceType.Knight:
                score = 10;
                break;
            case PieceType.Pawn:
                score = 1;
                break;
            default:
                score = 0;
                break;
        }

        return score * count;
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

    static int Max(int a, int b)
    {
        return a > b ? a : b;
    }

    static int Min(int a, int b)
    {
        return a < b ? a : b;
    }
}