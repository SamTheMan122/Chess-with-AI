using Cecs475.BoardGames.Model;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Collections.Generic;
using System;

namespace Cecs475.BoardGames.Chess.AvaloniaView {
    class ChessSquareBackgroundConverter : IMultiValueConverter
    {
		//private static readonly IBrush HIGHLIGHT_BRUSH = Brushes.Red;
		//private static readonly IBrush CORNER_BRUSH = Brushes.Green;
		//private static readonly IBrush SIDE_BRUSH = Brushes.LightGreen;
		//private static readonly IBrush DANGER_BRUSH = Brushes.PaleVioletRed;
		private static readonly IBrush DEFAULT_BRUSH = Brushes.LightBlue;
		private static readonly IBrush WHITESQUARES = Brushes.AntiqueWhite;
		private static readonly IBrush BLACKSQUARES = Brushes.Tan;
		private static readonly IBrush HIGHLIGHT_BRUSH = Brushes.Yellow;
        private static readonly IBrush SELECTED_BRUSH = Brushes.Red;
        private static readonly IBrush KINGCHECK_BRUSH = Brushes.Yellow;
        private static readonly IBrush STARTHOVER_BRUSH = Brushes.LightGreen;



        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) {
            // This converter will receive two properties: the Position of the square, and whether it
            // is being hovered.
            if (values.Count < 5 || values[0] is not BoardPosition pos)
            {
                return null;
                throw new ArgumentException("Converter must be bound to a BoardPosition and 4 booleans");
            }

            if (values[1] is not bool isKingInCheck)
            {
                return null;
                throw new ArgumentException("Expected bool for IsKingInCheck");
            }

            if (values[2] is not bool isSelected)
            {
                return null;
                throw new ArgumentException("Expected bool for IsSelected");
            }

            if (values[3] is not bool isHighlighted)
            {
                return null;
                throw new ArgumentException("Expected bool for IsHovered");
            }

            if (values[4] is not bool isPossibleMoveHover)
            {
                return null;
                throw new ArgumentException("Expected bool for IsPossibleMoveHover");
            }

            if (isSelected)
            {
                return SELECTED_BRUSH;
            }

            if (isKingInCheck)
            {
                return KINGCHECK_BRUSH;
            }

            if (isPossibleMoveHover)
            {
                return STARTHOVER_BRUSH;
            }

            if (isHighlighted)
            {
                return HIGHLIGHT_BRUSH;
            }


            // Returns Light/Dark Color based on position
            if ((pos.Row % 2 == 0 && pos.Col % 2 == 0) || (pos.Row % 2 != 0 && pos.Col % 2 != 0))
			{
				return WHITESQUARES;
			}
			else
			{
				return BLACKSQUARES;
			}

			return DEFAULT_BRUSH;
			
		}

    }
}