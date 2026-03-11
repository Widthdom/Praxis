namespace Praxis.Services;

public interface IErrorLogger
{
    void Log(Exception exception, string context);
    void LogInfo(string message, string context);
}
