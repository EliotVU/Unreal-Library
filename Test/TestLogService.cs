using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Services;

namespace Eliot.UELib.Test;

public class TestLogService : ILogService
{
    public void Log(string text)
    {
        //Console.WriteLine(text);
    }

    public void Log(string format, params object[] arg)
    {
        //Console.WriteLine(format, arg);
    }

    public void SilentException(Exception exception)
    {
        Assert.Fail(exception.ToString());
    }

    public void SilentAssert(bool assert, string message)
    {
        if (!assert) Console.Error.WriteLine(message);
        //Assert.IsTrue(assert, message);
    }
}
