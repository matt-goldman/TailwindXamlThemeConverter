namespace TailwindXamlThemeConverter;

public static class Headers
{
    public const string Wpf = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">";

    public const string Maui = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/dotnet/2021/maui""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">";

    public const string Uwp = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:ui=""using:Microsoft.UI.Xaml.Controls"">";

    public const string Uno = Uwp;

    public const string Avalonia = @"<ResourceDictionary xmlns=""https://github.com/avaloniaui""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">";
}

