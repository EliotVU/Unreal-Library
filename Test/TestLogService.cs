using UELib.Services;

namespace Eliot.UELib.Test;

public class TestLogService : ILogService
{
    public void Log(string text)
    {
#if DEBUG
        Console.WriteLine(text);
#endif
    }

    public void Log(string format, params object?[] arg)
    {
#if DEBUG
        Console.WriteLine(format, arg);
#endif
    }

    public void LogTrace(object? value)
    {
        System.Diagnostics.Trace.WriteLine(value);
    }

    public void LogTrace(string format, params object?[] arg)
    {
        ArgumentNullException.ThrowIfNull(arg);

        System.Diagnostics.Trace.WriteLine(string.Format(format, arg));
    }

    public void SilentException(Exception exception)
    {
        throw exception;
    }

    public void SilentAssert(bool condition, string? message)
    {
        if (!condition) Console.Error.WriteLine(message);
        //Assert.IsTrue(assert, message);
    }
}
