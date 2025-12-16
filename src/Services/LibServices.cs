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

    [Conditional("TRACE")]
    public static void Trace(string text)
    {
        LogService.LogTrace(text);
    }

    [Conditional("TRACE")]
    public static void Trace(object? value)
    {
        LogService.LogTrace(value);
    }

    [Conditional("TRACE")]
    public static void Trace(string format, params object?[] arg)
    {
        LogService.LogTrace(string.Format(format, arg));
    }
}
