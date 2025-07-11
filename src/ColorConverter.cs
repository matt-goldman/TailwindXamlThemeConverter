namespace TwToXaml;

public class ColorConverter
{
    public static string OklchToHex(double L, double C, double hDeg)
    {
        double a = Math.Cos(hDeg * Math.PI / 180.0) * C;
        double b = Math.Sin(hDeg * Math.PI / 180.0) * C;

        // Convert OKLab to LMS
        double l_ = L + 0.3963377774 * a + 0.2158037573 * b;
        double m_ = L - 0.1055613458 * a - 0.0638541728 * b;
        double s_ = L - 0.0894841775 * a - 1.2914855480 * b;

        double l = l_ * l_ * l_;
        double m = m_ * m_ * m_;
        double s = s_ * s_ * s_;

        double r = +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
        double g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
        double bC = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;

        // Convert linear RGB to sRGB
        r = r <= 0.0031308 ? 12.92 * r : 1.055 * Math.Pow(r, 1.0 / 2.4) - 0.055;
        g = g <= 0.0031308 ? 12.92 * g : 1.055 * Math.Pow(g, 1.0 / 2.4) - 0.055;
        bC = bC <= 0.0031308 ? 12.92 * bC : 1.055 * Math.Pow(bC, 1.0 / 2.4) - 0.055;

        // Clamp
        r = Math.Max(0, Math.Min(1, r));
        g = Math.Max(0, Math.Min(1, g));
        bC = Math.Max(0, Math.Min(1, bC));

        int rHex = (int)Math.Round(r * 255);
        int gHex = (int)Math.Round(g * 255);
        int bHex = (int)Math.Round(bC * 255);

        return $"#{rHex:X2}{gHex:X2}{bHex:X2}";
    }
}