﻿using Cecs475.BoardGames.Model;

#pragma warning disable 1591 // disable warning about missing XML documentation.
namespace Cecs475.BoardGames.Chess.Model
{
    /// <summary>
    /// Represents a single move to be applied to a chess board.
    /// </summary>
    public class ChessMove : IGameMove, IEquatable<ChessMove>
    {
        /// <summary>
        /// The starting position of the move.
        /// </summary>
        public BoardPosition StartPosition { get; }

        public ChessPieceType PieceType { get; set; }

        /// <summary>
        /// The ending position of the move.
        /// </summary>
        public BoardPosition EndPosition { get; }

        /// <summary>
        /// The type of move being applied.
        /// </summary>
        public ChessMoveType MoveType { get; set; }

        /// <inheritdoc />
        public int Player { get; set; }

        /// <summary>
        /// Constructs a ChessMove that moves a piece from one position to another
        /// </summary>
        /// <param name="start">the starting position of the piece to move</param>
        /// <param name="end">the position where the piece will end up</param>
        /// <param name="moveType">the type of move represented</param>
        public ChessMove(BoardPosition start, BoardPosition end, ChessMoveType moveType = ChessMoveType.Normal)
        {
            StartPosition = start;
            EndPosition = end;
            MoveType = moveType;
        }

        public ChessMove(BoardPosition start, BoardPosition end, ChessPieceType pieceType, ChessMoveType moveType = ChessMoveType.Normal)
        {
            StartPosition = start;
            EndPosition = end;
            PieceType = pieceType;
            MoveType = moveType;


        }

        bool IEquatable<IGameMove>.Equals(IGameMove? other)
        {
            return other is ChessMove m && this.Equals(m);
        }

        /// <summary>
        /// True if both moves start and end at the same positions.
        /// </summary>
        public virtual bool Equals(ChessMove? other)
        {
            return other is not null && StartPosition.Equals(other.StartPosition)
                && EndPosition.Equals(other.EndPosition);
        }

        /// <summary>
        /// True if both moves start and end at the same positions.
        /// </summary>
        public override bool Equals(object? other)
        {
            return other is ChessMove rhs && Equals(rhs);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StartPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ EndPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)MoveType;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"({PositionToString(StartPosition)}, {PositionToString(EndPosition)})";
        }

        internal static string PositionToString(BoardPosition pos)
        {
            return $"{(char)(pos.Col + 'a')}{8 - pos.Row}";
        }
    }

    /// <summary>
    /// Represents a ChessMove that is promoting a pawn to a chosen piece type.
    /// </summary>
    public class PawnPromotionChessMove : ChessMove
    {
        /// <summary>
        /// The ChessPieceType that the pawn will be promoted to.
        /// </summary>
        public ChessPieceType SelectedPromotion { get; }

        /// <summary>
        /// Constructs a ChessMove that moves a pawn to the final rank and promotes it to a chosen piece type.
        /// </summary>
        public PawnPromotionChessMove(BoardPosition start, BoardPosition end, ChessPieceType promoted)
            : base(start, end, ChessMoveType.PawnPromote)
        {
            SelectedPromotion = promoted;
        }

        public override bool Equals(ChessMove? other)
        {
            return other is PawnPromotionChessMove promo &&
                   StartPosition == promo.StartPosition && EndPosition == promo.EndPosition
                   && SelectedPromotion == promo.SelectedPromotion;
        }

        public override string ToString()
        {
            return $"{StartPosition} to {EndPosition}, promotion {SelectedPromotion}";
        }

    }
}
