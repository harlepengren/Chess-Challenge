using System;
using System.Collections;
using ChessChallenge.API;

namespace BoardAnalysis.Application
{
    public struct ScoreStruct
    {
        public float centerScore;
        public float pieceScore;
        public float rooksScore;
        public float checkmateScore;
        public float unprotectedScore;
    }

	public class Evaluate
	{
		Board board;

		public Evaluate(string inputFEN="")
		{
			if(inputFEN != "")
			{
				LoadFEN(inputFEN);
			}
		}

		public void LoadFEN(string inputFEN)
		{
			board = Board.CreateBoardFromFEN(inputFEN);
		}

        public ScoreStruct EvaluatePosition()
        {
            // Do we already know the score for this board
            LUT boardScore;
            ScoreStruct score;

            // We don't know it yet
            boardScore.IsWhiteToMove = board.IsWhiteToMove;
            boardScore.score = 0;

            // We may need to adjust the weights of these
            // Who controls the center?
            float centerWeight = 1 + 2 / board.PlyCount;
            boardScore.score += centerWeight * CenterScore(board);
            score.centerScore = centerWeight * CenterScore(board);

            // Decrease score for each unprotected piece
            boardScore.score -= UnprotectedPieces(board);
            score.unprotectedScore = UnprotectedPieces(board);

            // Piece score
            boardScore.score += 3 * (ScoreBoard(board, board.IsWhiteToMove) - ScoreBoard(board, !board.IsWhiteToMove));
            score.pieceScore = 3 * (ScoreBoard(board, board.IsWhiteToMove) - ScoreBoard(board, !board.IsWhiteToMove));

            // Linked rooks
            boardScore.score += 0.5f * LinkedRooks(board);
            score.rooksScore = 0.5f * LinkedRooks(board);

            score.checkmateScore = 0;
            if (board.IsInCheck())
            {
                // Who is in check?
                if (board.SquareIsAttackedByOpponent(board.GetKingSquare(board.IsWhiteToMove)))
                {
                    boardScore.score -= 5;
                    score.checkmateScore = -5;
                }
                else
                {
                    boardScore.score += 2;
                    score.checkmateScore = 2;
                }
            }


            // Checkmate
            score.checkmateScore += (board.IsInCheckmate()) ? 100 : 0;
            boardScore.score += (board.IsInCheckmate()) ? 100 : 0;

            

            return score;
        }

        float CenterScore(Board board)
        {
            // 3 Points for pieces in the center four squares
            // 2 points for pieces in the next outer square
            // 1 point for every piece attacking a center square

            // 3 points for every piece in the center four squares
            ulong bitboard = (board.IsWhiteToMove) ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
            ulong centerBits = 0x1818000000 & bitboard;
            float score = BitboardHelper.GetNumberOfSetBits(centerBits) * 3;

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

            return score / 22;
        }

        float UnprotectedPieces(Board board)
        {
            int score = 0;
            ulong pieces;

            // 1 for every piece that is unprotected
            pieces = (board.IsWhiteToMove) ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
            while (pieces > 0)
            {
                int index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces);

                // convert bitboard index to square and check if square is attacked
                // if attacked, how much support do we have?
                if (board.SquareIsAttackedByOpponent(new Square(index)))
                {
                    score += 1;
                    if (board.TrySkipTurn())
                    {
                        score -= 1;
                        board.UndoSkipTurn();
                    }
                }
            }

            return score / 16;
        }

        float LinkedRooks(Board board)
        {
            float score = 0;

            // Checks whether rooks are linked. If so, gives 5 points
            // 1) Get the rooks
            PieceList rooks = board.GetPieceList(PieceType.Rook, board.IsWhiteToMove);

            if (rooks.Count == 2)
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

        int ScoreBoard(Board board, bool isWhite)
        {
            int score = 0;

            // Who has the best pieces on the board?
            // {Q=20, R=15, B=8, N=8, P=1}
            score += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Queen, isWhite)) * 20 +
                     BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Rook, isWhite)) * 15 +
                     BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Bishop, isWhite)) * 10 +
                     BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Knight, isWhite)) * 8 +
                     BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Pawn, isWhite)) * 1;

            return score / 94;
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
}

