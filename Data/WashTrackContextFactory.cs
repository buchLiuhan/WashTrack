using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WashTrack.Data
{
    // Exists only for the EF Core command-line tools (dotnet ef migrations add
    // / database update). Those run without starting MAUI, so they can't get a
    // context from MauiProgram's DI container and would otherwise fail. EF
    // finds this class automatically — nothing in the app ever calls it.
    public class WashTrackContextFactory : IDesignTimeDbContextFactory<WashTrackContext>
    {
        public WashTrackContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WashTrackContext>();

            // Deliberately a plain relative path, not the LocalApplicationData
            // one OnConfiguring uses at runtime: migrations only need a file to
            // read the schema shape from. This creates/uses washtrack.db in the
            // project folder and never touches the real user database.
            optionsBuilder.UseSqlite("Data Source=washtrack.db");

            return new WashTrackContext(optionsBuilder.Options);
        }
    }
}