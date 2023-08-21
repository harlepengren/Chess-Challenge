using System;
using System.Collections;
using ChessChallenge.API;
using static System.Formats.Asn1.AsnWriter;

namespace BoardAnalysis.Application
{
    public struct ScoreStruct
    {
        public float centerScore;
        public float oppCenterScore;
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

        public GameInfo EvaluatePosition()
        {
            GameInfo score = new GameInfo();

            score.nextTurn = (board.IsWhiteToMove) ? 'w' : 'b';

            score.centerScore = CenterScore(board);
            if (board.TrySkipTurn())
            {
                score.oppCenterScore = CenterScore(board);
                board.UndoSkipTurn();
            }

            score.centerAttackScore = CenterAttackScore(board);
            if (board.TrySkipTurn())
            {
                score.oppAttackScore = CenterAttackScore(board);
            }

            score.slidingEdgeScore = EdgeScore(board);

            // Decrease score for each unprotected piece
            score.unprotectedScore = UnprotectedPieces(board);

            // Piece score
            score.pieceScore = ScoreBoard(board, board.IsWhiteToMove);
            score.oppPieceScore = ScoreBoard(board, !board.IsWhiteToMove);

            // Linked rooks
            score.rookScore = LinkedRooks(board);

            score.checkmateScore = check(board);

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

      

            return score;
        }

        float CenterAttackScore(Board board)
        {
            float score=0;

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

        float EdgeScore(Board board)
        {
            float score = 0;

            // -1 point for bishop, queen, and knight on the edge
            score -= BitboardHelper.GetNumberOfSetBits((board.GetPieceBitboard(PieceType.Queen, board.IsWhiteToMove) |
                        board.GetPieceBitboard(PieceType.Bishop, board.IsWhiteToMove) |
                        board.GetPieceBitboard(PieceType.Knight, board.IsWhiteToMove)) &
                        0xff818181818181ff);

            return score;

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
                Square currentSquare = new Square(index);
                if (board.SquareIsAttackedByOpponent(currentSquare))
                {
                    score += 1;
                    if (board.TrySkipTurn())
                    {
                        if (board.SquareIsAttackedByOpponent(currentSquare))
                        {
                            score -= 1;
                        }

                        board.UndoSkipTurn();
                    }
                }
            }

            return score;
        }

        float LinkedRooks(Board board)
        {
            float score = 0;

            ulong rooks = board.GetPieceBitboard(PieceType.Rook, board.IsWhiteToMove);

            if (BitboardHelper.GetNumberOfSetBits(rooks) == 2)
            {
                int[] rookIndex = new int[2];
                rookIndex[0] = BitboardHelper.ClearAndGetIndexOfLSB(ref rooks);
                rookIndex[1] = BitboardHelper.ClearAndGetIndexOfLSB(ref rooks);

                bool sameRow = rookIndex[0] / 8 == rookIndex[1] / 8;
                bool sameColumn = rookIndex[0] % 8 == rookIndex[1] % 8;
                if (sameRow || sameColumn)
                {
                    ulong blocker = 0x0;
                    ulong bitboard = board.AllPiecesBitboard;

                    int adjustment = (sameRow) ? 1 : 8;

                    for (int index = rookIndex[0] + 1; index < rookIndex[1]; index += adjustment)
                    {
                        blocker |= ((bitboard >> index) & 0x1);
                    }

                    score += (blocker == 0) ? 1 : 0;
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

            return score;
        }

        int check(Board board)
        {
            int score = 0;
            if (board.IsInCheck())
            {
                score -= 5;
            }

            // Checkmate
            score += (board.IsInCheckmate()) ? 100 : 0;

            if (board.TrySkipTurn())
            {
                if (board.IsInCheck())
                {
                    score += 5;
                }
                board.UndoSkipTurn();
            }

            return score;
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

