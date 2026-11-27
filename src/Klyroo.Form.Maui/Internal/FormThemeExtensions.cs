using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.Form.Maui;

/// <summary>Binds colors to the light and dark values of a <see cref="KlyThemeColor"/>, so a view follows the app theme without any code of its own.</summary>
internal static class FormThemeExtensions
{
    public static void SetThemeColor(this BindableObject target, BindableProperty property, KlyThemeColor color) =>
        target.SetAppThemeColor(property, color.Light, color.Dark);

    public static void SetStroke(this Border border, KlyThemeColor color) =>
        border.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(color.Light), new SolidColorBrush(color.Dark));

    public static void SetRadius(this Border border, double radius) =>
        border.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(radius) };

    /// <summary>The corner radius of a fraction of <see cref="KlyTheme.Radius"/>, as <c>rounded-(--klyroo-radius)</c> and its variants on the web.</summary>
    public static double RadiusOf(double fraction) => KlyTheme.Radius * fraction;
}
