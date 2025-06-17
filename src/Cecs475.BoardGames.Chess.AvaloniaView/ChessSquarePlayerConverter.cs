using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Cecs475.BoardGames.Chess.AvaloniaView {
	public class ChessSquarePlayerConverter : IValueConverter {
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
			if (value is not int player) {
				throw new ArgumentException(nameof(value));
			}
			if (player == 0) {
				return null;
			}
			return null;
			
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
