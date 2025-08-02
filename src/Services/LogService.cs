using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace UELib.Services;

public interface ILogService
{
    void Log(string text);
    void Log(string format, params object?[] arg);
    void SilentException(Exception exception);

#if NET8_0_OR_GREATER
    void SilentAssert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null);
#else
    void SilentAssert(bool condition, string? message = null);
#endif
}

public class DefaultLogService : ILogService
{
    public void Log(string text)
    {
        Console.WriteLine(text);
    }

    public void Log(string format, params object?[] arg)
    {
        Console.WriteLine(format, arg);
    }

    public void SilentException(Exception exception)
    {
        Console.Error.WriteLine(exception);
    }

    public void SilentAssert(bool condition, string? message = null)
    {
#if STRICT
        Contract.Assert(assert, message);
#else
        if (!condition && string.IsNullOrEmpty(message))
        {
            Console.Error.WriteLine(message);
        }
#endif
    }
}
