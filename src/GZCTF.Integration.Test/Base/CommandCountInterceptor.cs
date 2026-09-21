using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GZCTF.Integration.Test.Base;

/// <summary>
/// Records the database commands the application executes so release gates can assert
/// that a skill tree detail request stays bounded as the category graph grows.
/// </summary>
public sealed class CommandCountInterceptor : DbCommandInterceptor
{
    private static readonly ConcurrentBag<string> Commands = [];

    public static void Reset() => Commands.Clear();

    public static IReadOnlyCollection<string> Recorded => Commands.ToArray();

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Commands.Add(command.CommandText);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken token = default)
    {
        Commands.Add(command.CommandText);
        return base.ReaderExecutedAsync(command, eventData, result, token);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Commands.Add(command.CommandText);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken token = default)
    {
        Commands.Add(command.CommandText);
        return base.NonQueryExecutedAsync(command, eventData, result, token);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Commands.Add(command.CommandText);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken token = default)
    {
        Commands.Add(command.CommandText);
        return base.ScalarExecutedAsync(command, eventData, result, token);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        Commands.Add(command.CommandText);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command, CommandErrorEventData eventData, CancellationToken token = default)
    {
        Commands.Add(command.CommandText);
        return base.CommandFailedAsync(command, eventData, token);
    }
}
