using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cecs475.BoardGames.Model;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Collections.Generic;
using System;
using System;
using System.Globalization;
using Cecs475.BoardGames.Chess.Model;

namespace Cecs475.BoardGames.Chess.AvaloniaView
{
    public class ChessSquareImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ChessPiece piece && piece.PieceType != ChessPieceType.Empty)
            {
                string color = piece.Player == 1 ? "white" : "black";
                string type = piece.PieceType.ToString().ToLower();
                string src = $"{color}-{type}";

                return new Bitmap(AssetLoader.Open(new Uri($"avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/{src}.png")));
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}