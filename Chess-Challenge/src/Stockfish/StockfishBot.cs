using System;
using ChessChallenge.API;
using Stockfish.NET.Core;

namespace StockfishBot
{
    public class StockfishBot : IChessBot
    {
        Stockfish.NET.Core.Stockfish sf;

        public StockfishBot()
        {
            sf = new Stockfish.NET.Core.Stockfish("/usr/local/bin/stockfish");
        }

        public Move Think(Board board, Timer timer)
        {
            sf.SetFenPosition(board.GetFenString());
            
            string bestMove = sf.GetBestMove();

            Move sfMove = new Move(bestMove, board);

            return sfMove;
        }
    }
}

