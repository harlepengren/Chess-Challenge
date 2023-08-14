using System;
using ChessChallenge.API;

namespace BoardAnalysis.Application
{
    public struct ScoreStruct
    {
        public int centerScore;
        public int pieceScore;
        public int rooksScore;
        public int checkmateScore;
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
            ScoreStruct scoreStruct = new ScoreStruct();

            // Who controls the center?
            scoreStruct.centerScore = CenterScore(board);

            // Decrease score for each unprotected piece
            //int unprotectedScore = UnprotectedPieces();

            // Piece score
            scoreStruct.pieceScore = ScoreBoard(board);

            // Linked rooks
            scoreStruct.rooksScore = LinkedRooks(board);

            // Checkmate
            scoreStruct.checkmateScore = (board.IsInCheckmate()) ? 100 : 0;


            return scoreStruct;
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
                if (currentPiece.PieceType != PieceType.None)
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

            foreach (Square currentSquare in centerSquares)
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
            foreach (PieceList currentList in pieceList)
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
    }
}

