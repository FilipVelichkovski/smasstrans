using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Converters;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public class LineRouteIdConverter : IValueConverter
{
    public static readonly LineRouteIdConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, 
        CultureInfo culture)
    {
        if (value is LineRouteId linerouteID  
                                       && targetType.IsAssignableTo(typeof(string)))
        {
           var res= Task.Run(async () => await MainView.client.GetLineInfo(linerouteID)).GetAwaiter().GetResult();
            return res.Name;
        }
        return new BindingNotification(new InvalidCastException(), 
            BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, 
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}