using Microsoft.EntityFrameworkCore;
using IdentityService.Models;

namespace IdentityService.Data
{
    public class IdentityDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        {
        }
    }

}
