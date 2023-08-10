// TurtleBot: Develops slowly and defensively. Waits for the opponent to run out of time.
// Control the center of the board.
// No sacrifices
// Moves are evaluated based on level of protection
// Watch for forks

using ChessChallenge.API;
using System.Linq;

public class MyBot : IChessBot
{
    const int MAX_DEPTH = 0;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        int[] scores = new int[moves.Length];

        for(int index=0; index<moves.Length; ++index)
        {
            board.MakeMove(moves[index]);
            scores[index] = EvaluatePosition(board,MAX_DEPTH);

            // Special move cases
            scores[index] += (moves[index].IsPromotion) ? 20 : 0;
            scores[index] += (moves[index].IsCastles) ? 10 : 0;

            board.UndoMove(moves[index]);
        }

        int maxScore = scores.Max();
        int maxIndex = scores.ToList<int>().IndexOf(maxScore);

        return moves[maxIndex];
    }

    int EvaluatePosition(Board board, int depth)
    {
        int max_score = 0;

        if(depth == 0)
        {


            // We may need to adjust the weights of these
            // Who controls the center?
            max_score += CenterScore(board);

            // Decrease score for each unprotected piece
            max_score -= UnprotectedPieces();

            // Piece score
            max_score += ScoreBoard();

            // king-side Castle move should be given high weight, queenside, slightly less

            return max_score;
        }

        Move[] moves = board.GetLegalMoves();
        foreach(Move currentMove in moves)
        {
            int score = int.MinValue;

            for (int index = 0; index < moves.Length; ++index)
            {
                board.MakeMove(moves[index]);
                score = EvaluatePosition(board, depth - 1);

                if(score > max_score)
                {
                    max_score = score;
                }

                board.UndoMove(moves[index]);
            }

        }

        int opponentMove = (depth % 2 == 0) ? 1 : -1;
        return max_score * opponentMove;
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
}