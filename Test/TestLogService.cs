using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Services;

namespace Eliot.UELib.Test;

public class TestLogService : ILogService
{
    public void SilentException(Exception exception)
    {
        Assert.Fail(exception.ToString());
    }
}