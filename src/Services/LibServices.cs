using System.Diagnostics;

namespace UELib.Services;

public static class LibServices
{
    public static ILogService LogService = new DefaultLogService();

    [Conditional("DEBUG")]
    public static void Debug(string text)
    {
        LogService.Log(text);
    }

    [Conditional("DEBUG")]
    public static void Debug(string format, params object?[] arg)
    {
        LogService.Log(format, arg);
    }
}