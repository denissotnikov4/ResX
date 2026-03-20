using ResX.Common.Persistence;

namespace ResX.Users.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly UsersDbContext _context;

    public UnitOfWork(UsersDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}