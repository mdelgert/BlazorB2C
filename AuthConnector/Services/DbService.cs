using Microsoft.EntityFrameworkCore;
using AuthConnector.Models;

namespace AuthConnector.Services
{
    public class DbService : DbContext
    {
        public DbService(DbContextOptions<DbService> options)
            : base(options)
        {
        }

        public DbSet<LogModel> Logs { get; set; }
    }
}
