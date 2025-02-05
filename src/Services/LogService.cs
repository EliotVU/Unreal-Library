using System;

namespace UELib.Services;

public interface ILogService
{
    void SilentException(Exception exception);
}

public class DefaultLogService : ILogService
{
    public void SilentException(Exception exception)
    {
        Console.Error.Write(exception);
    }
}