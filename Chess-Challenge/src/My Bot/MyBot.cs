// TurtleBot: Develops slowly and defensively. Waits for the opponent to run out of time.
// Control the center of the board.
// No sacrifices
// Moves are evaluated based on level of protection
// Watch for forks

using ChessChallenge.API;
using System.Linq;

public class MyBot : IChessBot
{
    const int MAX_DEPTH = 3;

    public Move Think(Board board, Timer timer)
    {

        Move[] moves = board.GetLegalMoves();
        int[] scores = new int[moves.Length];

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
        int score = 0;

        if(depth == 0)
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
        int score = 0;

        // We may need to adjust the weights of these
        // Who controls the center?
        score += CenterScore(board);

        // Decrease score for each unprotected piece
        score -= UnprotectedPieces();

        // Piece score
        score += ScoreBoard();

        // king-side Castle move should be given high weight, queenside, slightly less

        return score;
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
        PieceList knightList = board.GetPieceList(PieceType.Knight, board.IsWhiteToMove);

        foreach(Piece currentPiece in knightList)
        {
            if(currentPiece.Square.File == 0 || currentPiece.Square.File == 7)
            {
                score -= 1;
            }
        }

        PieceList bishopList = board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove);

        foreach (Piece currentPiece in bishopList)
        {
            if (currentPiece.Square.File == 0 || currentPiece.Square.File == 7)
            {
                score -= 1;
            }
        }

        PieceList queenList = board.GetPieceList(PieceType.Queen, board.IsWhiteToMove);

        foreach (Piece currentPiece in queenList)
        {
            if (currentPiece.Square.File == 0 || currentPiece.Square.File == 7)
            {
                score -= 1;
            }
        }


        return score;
    }

    int UnprotectedPieces()
    {
        int score = 0;

        // 1 for every piece that is unprotected
        // foreach of our pieces
        // Get the position
        // If 0 of our pieces are attacking that square, subtract 1

        return score;
    }

    int ScoreBoard()
    {
        int score = 0;

        // Who has the best pieces on the board?
        // {Q=20, R=15, N=10, B=8, P=1}

        // Positive score means we have the best pieces, negative means they do

        return score;
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