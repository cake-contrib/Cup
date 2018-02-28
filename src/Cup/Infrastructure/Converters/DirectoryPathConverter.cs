using System.ComponentModel;
using System.Globalization;
using Spectre.System.IO;

namespace Cup.Infrastructure.Converters
{
    internal sealed class DirectoryPathConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                return new DirectoryPath(stringValue);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}