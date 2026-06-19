using ArenaInfrastructure.Data;
using ArenaInfrastructure.Data.DataSeeding;

namespace ArenaMVC.Services;

public interface IDashboardDataSeeder
{
    Task SeedAsync(bool forceReseed = false);
}

public class DashboardDataSeederService : IDashboardDataSeeder
{
    private readonly AppDbContext _context;

    public DashboardDataSeederService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(bool forceReseed = false)
    {
        await DashboardDataSeeder.SeedAsync(_context, forceReseed);
    }
}
