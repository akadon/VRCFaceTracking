using System.Globalization;
using Avalonia.Data.Converters;
using VRCFaceTracking.Core.Params.Data.Mutation;

namespace VRCFaceTracking.Linux.Converters;

public class MutationTypeEqualConverter : IValueConverter
{
    public static readonly MutationTypeEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MutationPropertyType type && parameter is MutationPropertyType target && type == target;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
