using System;
using System.Globalization;
using System.Windows.Media;

namespace mBESS
{
    class MyConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            return (isChecked ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
