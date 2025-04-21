using System;

namespace UELib.Services;

public interface ILogService
{
    void Log(string text);
    void Log(string format, params object?[] arg);
    void SilentException(Exception exception);
    void SilentAssert(bool assert, string message);
}

public class DefaultLogService : ILogService
{
    public void Log(string text)
    {
        Console.WriteLine(text);
    }

    public void Log(string format, params object[] arg)
    {
        Console.WriteLine(format, arg);
    }

    public void SilentException(Exception exception)
    {
        Console.Error.WriteLine(exception);
    }

    public void SilentAssert(bool assert, string message)
    {
#if STRICT
        Contract.Assert(assert, message);
#else
        if (!assert)
        {
            Console.Error.WriteLine(message);
        }
#endif
    }
}
