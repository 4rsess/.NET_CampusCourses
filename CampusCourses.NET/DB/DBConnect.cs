using CampusCourses.NET.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusCourses.NET.DB
{
    public class DBConnect : DbContext
    {

        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<CampusCourse> CampusCourses { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DBConnect(DbContextOptions<DBConnect> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CampusCourse>(entity =>
            {
                entity.Property(e => e.Semester)
                    .HasConversion<string>();

                entity.Property(e => e.Status)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.Property(e => e.Status)
                    .HasConversion<string>();

                entity.Property(e => e.MidtermResult)
                    .HasConversion<string>();

                entity.Property(e => e.FinalResult)
                    .HasConversion<string>();
            });
        }

    }
}
