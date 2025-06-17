using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cecs475.BoardGames.Model
{
    public static class MinimaxOpponent
    {
        public static IGameMove FindBestMove(IGameBoard board, int depth = 1)
        {
            return FindBestMove(board, depth, true).aiMove!;
        }

        private static (long weight, IGameMove? aiMove) FindBestMove(IGameBoard board, int depth, bool isMaximizing)
        {
            if (depth == 0 || board.IsFinished)
            {
                return (board.BoardWeight, null);
            }

            long bestWeight = isMaximizing ? long.MinValue : long.MaxValue; // if maximizing, we want the highest weight, otherwise the lowest
            IGameMove? aiMove = null;

            foreach (var move in board.GetPossibleMoves())
            {
                board.ApplyMove(move);
                long childWeight = FindBestMove(board, depth - 1, !isMaximizing).weight;
                board.UndoLastMove();

                if (isMaximizing && childWeight > bestWeight)
                {
                    bestWeight = childWeight;
                    aiMove = move;
                }
                else if (!isMaximizing && childWeight < bestWeight)
                {
                    bestWeight = childWeight;
                    aiMove = move;
                }
            }

            return (bestWeight, aiMove);
        }
    }
}
