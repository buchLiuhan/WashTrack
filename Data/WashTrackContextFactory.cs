using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WashTrack.Data
{
    public class WashTrackContextFactory : IDesignTimeDbContextFactory<WashTrackContext>
    {
        public WashTrackContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WashTrackContext>();
            optionsBuilder.UseSqlite("Data Source=washtrack.db");

            return new WashTrackContext(optionsBuilder.Options);
        }
    }
}