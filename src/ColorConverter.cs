namespace TwToXaml;

public class ColorConverter
{
    public static string OklchToHex(double l, double c, double h)
    {
        double a = c * Math.Cos(h * Math.PI / 180);
        double b = c * Math.Sin(h * Math.PI / 180);

        double y = (l + 0.3963377774 * a + 0.2158037573 * b) / 100.0;
        double x = (l - 0.1055613458 * a - 0.0638541728 * b) / 100.0;
        double z = (l - 0.0894841775 * a - 1.2914855480 * b) / 100.0;

        double r = 4.0767416621 * x - 3.3077115913 * y + 0.2309699292 * z;
        double g = -1.2684380046 * x + 2.6097574011 * y - 0.3413193965 * z;
        double bb = -0.0041960863 * x - 0.7034186147 * y + 1.7076147010 * z;

        r = r <= 0.0031308 ? 12.92 * r : 1.055 * Math.Pow(r, 1.0 / 2.4) - 0.055;
        g = g <= 0.0031308 ? 12.92 * g : 1.055 * Math.Pow(g, 1.0 / 2.4) - 0.055;
        bb = bb <= 0.0031308 ? 12.92 * bb : 1.055 * Math.Pow(bb, 1.0 / 2.4) - 0.055;

        r = Math.Clamp(r, 0, 1);
        g = Math.Clamp(g, 0, 1);
        bb = Math.Clamp(bb, 0, 1);

        int rHex = (int)Math.Round(r * 255);
        int gHex = (int)Math.Round(g * 255);
        int bHex = (int)Math.Round(bb * 255);

        return $"#{rHex:X2}{gHex:X2}{bHex:X2}";
    }
}