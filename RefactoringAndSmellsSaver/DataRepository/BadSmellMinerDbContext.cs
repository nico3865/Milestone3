using RefactoringAndSmellsSaver.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace DataRepository
{
    public class BadSmellMinerDbContext : DbContext
    {
        public DbSet<Project> Projects { get; set; }

        public DbSet<Commit> Commits { get; set; }

        public DbSet<Refactoring> Refactorings { get; set; }

        public DbSet<OrganicClass> OrganicClasses { get; set; }

        public DbSet<OrganicMethod> OrganicMethods { get; set; }

        public DbSet<OrganicMetrics> OrganicMetrics { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Database/badsmellMiner.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrganicClass>().HasIndex(m=>m.FullyQualifiedName);
            
            modelBuilder.Entity<OrganicMethod>().HasIndex(m=>m.FullyQualifiedName);

            modelBuilder.Entity<OrganicSmell>().HasIndex(m=>m.Name);

            modelBuilder.Entity<Commit>().HasIndex(m=>m.CommitId);

            modelBuilder.Entity<Commit>().HasIndex(m=>m.ProjectId);

            modelBuilder.Entity<Commit>().HasIndex(m=>m.AuthorName);

            modelBuilder.Entity<Refactoring>().HasIndex(m=>m.CommitId);

            modelBuilder.Entity<Refactoring>().HasIndex(m=>m.Type);

            modelBuilder.Entity<Refactoring>().HasIndex(m=>m.ProjectId);
        }
    }
}