using System.Globalization;

namespace Chat_TCP_IP
{
    //Converter to change color of button if text field is not empty
    internal class StringToStyleConverter : IValueConverter
    {
        public Style Approved { get; set; }
        public Style Denied { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringField = (string)value;
            if (stringField == String.Empty || stringField == null)
                return Approved;
            return Denied;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as Style;

            if (status == Approved) return String.Empty;
            return "not empty";
        }
    }
}
