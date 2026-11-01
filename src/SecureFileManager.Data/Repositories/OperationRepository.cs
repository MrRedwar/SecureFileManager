using Microsoft.EntityFrameworkCore;
using SecureFileManager.Core.Entities;
using SecureFileManager.Core.Interfaces;
using SecureFileManager.Core.Results;

namespace SecureFileManager.Data.Repositories;

public sealed class OperationRepository(IDbContextFactory<AppDbContext> factory) : RepositoryBase(factory), IOperationRepository
{
    public Task<RepositoryResult<Operation>> AddAsync(Operation operation, CancellationToken cancellationToken = default) =>
        ExecuteAsync<Operation>(async db =>
        {
            if (operation.FileId is Guid fileId && !await db.Files.IgnoreQueryFilters().AnyAsync(
                x => x.Id == fileId && x.OwnerId == operation.UserId, cancellationToken))
                return RepositoryResult<Operation>.Fail(RepositoryStatus.Validation);
            db.Operations.Add(operation);
            await db.SaveChangesAsync(cancellationToken);
            return RepositoryResult<Operation>.Ok(operation);
        }, cancellationToken);

    public Task<RepositoryResult<IReadOnlyList<Operation>>> GetForUserAsync(Guid userId, int take = 100,
        CancellationToken cancellationToken = default) => ExecuteAsync<IReadOnlyList<Operation>>(async db =>
        {
            if (userId == Guid.Empty || take is < 1 or > 500)
                return RepositoryResult<IReadOnlyList<Operation>>.Fail(RepositoryStatus.Validation);
            return RepositoryResult<IReadOnlyList<Operation>>.Ok(await db.Operations.AsNoTracking()
                .Where(x => x.UserId == userId).OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                .Take(take).ToListAsync(cancellationToken));
        }, cancellationToken);
}
