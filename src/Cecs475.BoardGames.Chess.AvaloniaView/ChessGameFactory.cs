using Avalonia.Data.Converters;
using Cecs475.BoardGames.AvaloniaView;
using Cecs475.BoardGames.Model;
using System;
using System.Globalization;

namespace Cecs475.BoardGames.Chess.AvaloniaView {
	public class ChessGameFactory : IAvaloniaGameFactory {
		public string GameName {
			get {
				return "Chess";
			}
		}

		public IValueConverter CreateBoardAdvantageConverter() {
			// TODO: after creating a ChessAdvantageConverter, construct and return
			// an object of that class.
			return new InlineAdvantageConverter();

		}

		public IValueConverter CreateCurrentPlayerConverter() {
			// TODO: after creating a ChessCurrentPlayerConverter, construct and return
			// an object of that class.
			return new InlineCurrentPlayerConverter();
        }

        public IAvaloniaGameView CreateGameView()
        {
            return new ChessView();
        }

        // --- Inline Converter Classes ---
        private class InlineCurrentPlayerConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is not int player || player == 0)
                    return "None";
                return player == 1 ? "White" : "Black";
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class InlineAdvantageConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is not GameAdvantage adv)
                    return "Tie game";
                if (adv.Advantage == 0)
                    return "Tie game";
                string player = adv.Player == 1 ? "White" : "Black";
                return $"{player} has a +{adv.Advantage} advantage";
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
	}
}
