using IngestionService.Entities;
using Microsoft.EntityFrameworkCore;

namespace IngestionService.Persistence
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }


        public DbSet<Sensor> Sensors { get; set; }

        public DbSet<Metric> Metrics { get; set; }
    }
}
