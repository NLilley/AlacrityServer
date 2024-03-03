using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace AlacrityCore.Infrastructure;

public interface IALogger
{
    void Trace(string msg);
    void Trace(string msg, params object[] args);
    void Debug(string msg);
    void Debug(string msg, params object[] args);
    void Info(string msg);
    void Info(string msg, params object[] args);
    void Warn(string msg);
    void Warn(string msg, params object[] args);
    void Error(string msg);
    void Error(string msg, params object[] args);
    void Error(Exception ex);
    void Error(Exception ex, params object[] args);
    void Error(Exception ex, string msg);
    void Error(Exception ex, string msg, params object[] args);
}

public class ALogger : IALogger
{
    private readonly static string _executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    private readonly static string _logDirectory = Path.Join(_executableDir, "/Logs");

    // Use the SeriLog objects rather than our own IAlogger in order to be compaitble with the SeriLog AspNetCore middleware.
    private static Logger _aspNetCoreLogger;
    public static Logger GetAspNetCoreLogger(IConfiguration configuration)
    {
        if (_aspNetCoreLogger != null)
            return _aspNetCoreLogger;

        var logPath = Path.Join(_logDirectory, "server-.log");
        _aspNetCoreLogger = new LoggerConfiguration()                        
            .ReadFrom.Configuration(configuration)            
            .WriteTo.Async(a =>
            {
                a.File(                    
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    flushToDiskInterval: TimeSpan.FromSeconds(5)
                ).WriteTo.Console();
            })            
            .CreateLogger();

        return _aspNetCoreLogger;
    }

    private static ALogger _backServiceLogger;
    public static ALogger GetBackServiceLogger()
    {
        if (_backServiceLogger != null)
            return _backServiceLogger;

        var logPath = Path.Join(_logDirectory, "backservice-.log");
        var seriLogger = new LoggerConfiguration()
        //.MinimumLevel.Information()
        .MinimumLevel.Warning()
        .WriteTo.Async(a =>
        {
            // Note: Async has a buffer behind, and will start dropping messages when it exceeds 10,000 messages!
            a.File(

                logPath,
                rollingInterval: RollingInterval.Day,
                flushToDiskInterval: TimeSpan.FromSeconds(5)
            )
                .WriteTo.Console();
        })
        .CreateLogger();

        _backServiceLogger = new ALogger(seriLogger);

        return _backServiceLogger;
    }

    private static ALogger _exchangeLogger;
    public static ALogger GetExchangeLogger()
    {
        if (_exchangeLogger != null)
            return _exchangeLogger;

        var logPath = Path.Join(_logDirectory, "exchange-.log");
        var seriLogger = new LoggerConfiguration()
            //.MinimumLevel.Information()
            .MinimumLevel.Warning()
            .WriteTo.Async(a =>
            {
                // Note: Async has a buffer behind, and will start dropping messages when it exceeds 10,000 messages!
                a.File(

                    logPath,
                    rollingInterval: RollingInterval.Day,
                    flushToDiskInterval: TimeSpan.FromSeconds(5)
                )
                    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning);
            })
            .CreateLogger();

        _exchangeLogger = new ALogger(seriLogger);

        return _exchangeLogger;
    }

    private readonly Logger _seriLogger;
    private ALogger(Logger seriLogger) => _seriLogger = seriLogger;

    public void Trace(string msg)
        => _seriLogger.Verbose(msg);

    public void Trace(string msg, params object[] args)
        => _seriLogger.Verbose(msg, args);

    public void Debug(string msg)
        => _seriLogger.Debug(msg);

    public void Debug(string msg, params object[] args)
        => _seriLogger.Debug(msg, args);

    public void Info(string msg)
        => _seriLogger.Information(msg);

    public void Info(string msg, params object[] args)
        => _seriLogger.Information(msg, args);

    public void Warn(string msg)
        => _seriLogger.Warning(msg);

    public void Warn(string msg, params object[] args)
        => _seriLogger.Warning(msg, args);

    public void Error(string msg)
        => _seriLogger.Error(msg);

    public void Error(string msg, params object[] args)
        => _seriLogger.Error(msg, args);

    public void Error(Exception ex)
        => _seriLogger.Error(ex, "Error:");

    public void Error(Exception ex, params object[] args)
        => _seriLogger.Error(ex, "Error:", args);

    public void Error(Exception ex, string msg)
        => _seriLogger.Error(ex, msg);

    public void Error(Exception ex, string msg, params object[] args)
        => _seriLogger.Error(ex, msg, args);

    public void Kill()
        => _seriLogger.Dispose();

    public async Task KillAsync()
        => await _seriLogger.DisposeAsync();
}