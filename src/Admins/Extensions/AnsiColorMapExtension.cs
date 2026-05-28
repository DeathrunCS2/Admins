namespace Admins.Extensions;

public static class AnsiColorMapExtension
{
    public const string Reset = "\u001b[0m";

    public const string Red = "\u001b[31m";
    public const string Green = "\u001b[32m";
    public const string Yellow = "\u001b[33m";
    public const string Blue = "\u001b[34m";
    public const string Magenta = "\u001b[35m";
    public const string Cyan = "\u001b[36m";
    public const string White = "\u001b[37m";
    public const string Gray = "\u001b[38;2;133;133;133m";

    public const string Peach = "\u001b[38;2;255;175;135m";
    
    public static string Color(string text, string color)
    {
        return $"{color}{text}{Reset}";
    }
}