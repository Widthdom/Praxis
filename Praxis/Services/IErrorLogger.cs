namespace Praxis.Services;

public interface IErrorLogger
{
    void Log(Exception exception, string context);
}
