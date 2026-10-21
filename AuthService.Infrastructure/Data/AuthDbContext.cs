using Microsoft.EntityFrameworkCore;
using AuthService.Models;

namespace AuthService.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User_Raj> Users { get; set; }
        public DbSet<Role_Raj> Roles { get; set; }
        public DbSet<Department_Raj> Departments { get; set; }
        public DbSet<PasswordReset_Raj> PasswordResets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role_Raj>().ToTable("Roles_Raj");
            modelBuilder.Entity<Department_Raj>().ToTable("Departments_Raj");
            modelBuilder.Entity<User_Raj>().ToTable("Users_Raj");
            modelBuilder.Entity<PasswordReset_Raj>()
                .ToTable("PasswordResets_Raj")
                .HasKey(p => p.UserId);

            // Global query filters or constraints if needed
            modelBuilder.Entity<User_Raj>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
