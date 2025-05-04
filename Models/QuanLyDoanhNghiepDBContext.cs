using Microsoft.EntityFrameworkCore;

namespace QuanLyDoanhNghiep.Models
{
    public class QuanLyDoanhNghiepDBContext : DbContext
    {
        public QuanLyDoanhNghiepDBContext(DbContextOptions<QuanLyDoanhNghiepDBContext> options) : base(options) { }
        public DbSet<Account> Account { get; set; }
        public DbSet<Company> Company { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<JobPosition> JobPosition { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<CV> CV { get; set; }
        public DbSet<Province> Province { get; set; }
        public DbSet<District> District { get; set; }
        public DbSet<Ward> Ward { get; set; }
        public DbSet<JobLocation> JobLocation { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<ITPositionCategory> ITPositionCategory { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().ToTable("Account");
            modelBuilder.Entity<Company>().ToTable("Company");
            modelBuilder.Entity<Employee>().ToTable("Employee");
            modelBuilder.Entity<JobPosition>().ToTable("JobPosition");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<CV>().ToTable("CV");
            modelBuilder.Entity<Province>().ToTable("Province");
            modelBuilder.Entity<District>().ToTable("District");
            modelBuilder.Entity<Ward>().ToTable("Ward");
            modelBuilder.Entity<JobLocation>().ToTable("JobLocation");
            modelBuilder.Entity<Notification>().ToTable("Notification");
            modelBuilder.Entity<ITPositionCategory>().ToTable("ITPositionCategory");

        }

    }
}
