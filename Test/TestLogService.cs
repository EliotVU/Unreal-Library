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

    public void SilentException(Exception exception)
    {
        Assert.Fail(exception.ToString());
    }

    public void SilentAssert(bool assert, string? message)
    {
        if (!assert) Console.Error.WriteLine(message);
        //Assert.IsTrue(assert, message);
    }
}
