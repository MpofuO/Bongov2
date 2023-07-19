using Bongo.Models;
using Bongo.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Bongo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        public DbSet<Session> Sessions { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Timetable> Timetables { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<ModuleColor> ModuleColors { get; set; }

        public DbSet<UserReview> UserReviews { get; set; }  
    }
}
