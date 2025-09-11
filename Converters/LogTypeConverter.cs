using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbapp.Converters
{
	public class LogTypeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return string.Empty;

			string? logType = value?.ToString();
			return logType switch
			{
				"I" => "In",
				"0" => "B-Out",
				"1" => "B-In",
				"O" => "Out",
				_ => string.Empty
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
