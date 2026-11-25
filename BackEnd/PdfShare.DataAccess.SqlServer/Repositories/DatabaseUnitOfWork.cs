using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;

namespace PdfShare.DataAccess.SqlServer.Repositories;

public sealed class DatabaseUnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> action, CancellationToken ct = default)
    {
        // Required with EnableRetryOnFailure: the whole transaction is retried, not one statement.
        var strategy = context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async token =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(token);
            var result = await action(token);
            if (result.IsSuccess)
            {
                await transaction.CommitAsync(token);
            }
            else
            {
                await transaction.RollbackAsync(token);
                context.ChangeTracker.Clear();
            }

            return result;
        }, ct);
    }
}
