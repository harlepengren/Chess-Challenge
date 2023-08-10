// Basic implementation of a min/max strategy

using ChessChallenge.API;

public class MinMaxBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        int[] scores = new int[moves.GetLength(0)];

        int maxScoreIndex = 0;

        for(int index=0; index < moves.GetLength(0); ++index)
        {
            board.MakeMove(moves[index]);
            scores[index] = MinMax(board, 2);

            if (scores[index] > scores[maxScoreIndex])
            {
                maxScoreIndex = index;
            }
            board.UndoMove(moves[index]);
        }

        return moves[maxScoreIndex];
    }

    private int MinMax(Board board, int depth, bool min=false)
    {
        if (depth == 0)
        {
            return EvaluateBoard(board);
        }

        int maxScore = int.MinValue;
        Move[] moves = board.GetLegalMoves();

        foreach (Move currentMove in moves)
        {
            board.MakeMove(currentMove);
            int score = MinMax(board, depth - 1, !min);
            if(score > maxScore)
            {
                maxScore = score;
            }
            board.UndoMove(currentMove);
        }

        if (min)
        {
            maxScore = -maxScore;
        }

        return maxScore;
    }

    int EvaluateBoard(Board currentBoard)
    {
        PieceList[] allPieces = currentBoard.GetAllPieceLists();
        int finalScore = 0;

        // For now, add points for white and subtract for black
        foreach (PieceList pieceList in allPieces)
        {
            foreach(Piece currentPiece in pieceList)
            {
                finalScore += PieceScore(currentPiece.PieceType, currentPiece.IsWhite);
            }
        }

        return finalScore;
    }

    int PieceScore(PieceType currentPiece, bool white)
    {
        int score = 0;

        switch (currentPiece)
        {
            case PieceType.Pawn:
                score = 1;
                break;
            case PieceType.Knight:
            case PieceType.Bishop:
                score = 3;
                break;
            case PieceType.Rook:
                score = 5;
                break;
            case PieceType.Queen:
                score = 9;
                break;
            default:
                return 0;
        }

        if (!white)
        {
            score = -score;
        }

        return score;
    }
}