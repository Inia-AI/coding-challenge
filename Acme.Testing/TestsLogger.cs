/*
 * Code from https://www.meziantou.net/how-to-get-asp-net-core-logs-in-the-output-of-xunit-tests.htm
*/

using Microsoft.Extensions.Logging;
using System.Text;
using Xunit.Abstractions;

namespace Acme.Testing;

/// <summary>
/// A logger that implements an <see cref="ILogger"/> interface and writes logs
/// to the test output.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestsLogger"/> class.
/// </remarks>
/// <param name="testOutputHelper">XUnit test output helper.</param>
/// <param name="scopeProvider">Logger scope provider.</param>
/// <param name="categoryName">The category name for the logger.</param>
public class TestsLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName) : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
    private readonly string _categoryName = categoryName;
    private readonly LoggerExternalScopeProvider _scopeProvider = scopeProvider;

    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="TestsLogger"/> class.
    /// </summary>
    /// <param name="testOutputHelper">XUnit test output helper.</param>
    /// <returns>A new instance of the <see cref="TestsLogger"/> class.</returns>
    public static ILogger CreateLogger(ITestOutputHelper testOutputHelper)
    {
        return new TestsLogger(testOutputHelper, new LoggerExternalScopeProvider(), "");
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TestsLogger"/> class for the
    /// specified category.
    /// </summary>
    /// <typeparam name="T">The type of the category.</typeparam>
    /// <param name="testOutputHelper">XUnit test output helper.</param>
    /// <returns>A new instance of the <see cref="TestsLogger"/> class.</returns>
    public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper)
    {
        return new TestsLogger<T>(testOutputHelper, new LoggerExternalScopeProvider());
    }

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return _scopeProvider.Push(state);
    }

    /// <summary>
    /// Checks if the specified log level is enabled.
    /// </summary>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return LogLevel <= logLevel;
    }

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <typeparam name="TState">A type representing the state of the log entry.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="state">The state of the log entry.</param>
    /// <param name="exception">The exception associated with the log entry.</param>
    /// <param name="formatter">A function to create a message from the state and exception.</param>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var sb = new StringBuilder();
        _ = sb.Append($"[{DateTime.Now:HH:mm:ss.fff}]")
            .Append(GetLogLevelString(logLevel))
            .Append($"[{_categoryName}] ")
            .Append(formatter(state, exception));

        if (exception is not null)
        {
            _ = sb.Append('\n').Append(exception);
        }

        // Append scopes
        _scopeProvider.ForEachScope(
            (scope, state) =>
                {
                    _ = state.Append("\n => ");
                    _ = state.Append(scope);
                }, sb);

        _testOutputHelper.WriteLine(sb.ToString());
    }

    /// <summary>
    /// Writes a simple log message.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void Log(string message)
    {
        Log(LogLevel.Information, new EventId(), message, null, (state, exception) => state.ToString());
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "[trce]",
            LogLevel.Debug => "[dbug]",
            LogLevel.Information => "[info]",
            LogLevel.Warning => "[warn]",
            LogLevel.Error => "[fail]",
            LogLevel.Critical => "[crit]",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }
}

/// <summary>
/// A logger that implements an <see cref="ILogger{T}"/> interface and writes logs
/// to the test output.
/// </summary>
/// <typeparam name="T">The type of the category.</typeparam>
public sealed class TestsLogger<T>(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider) : TestsLogger(testOutputHelper, scopeProvider, typeof(T).Name), ILogger<T>
{
}

/// <summary>
/// A logger provider that creates instances of the <see cref="TestsLogger"/> class.
/// </summary>
public sealed class TestsLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
    private readonly LoggerExternalScopeProvider _scopeProvider = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new TestsLogger(_testOutputHelper, _scopeProvider, categoryName);
    }

    public void Dispose()
    {
    }
}
