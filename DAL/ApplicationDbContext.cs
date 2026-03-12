using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<LabelClass> LabelClasses { get; set; }
        public DbSet<DataItem> DataItems { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Annotation> Annotations { get; set; }
        public DbSet<ReviewLog> ReviewLogs { get; set; }
        public DbSet<UserProjectStat> UserProjectStats { get; set; }
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ReviewChecklistItem> ReviewChecklistItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình Project - Manager
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany(u => u.ManagedProjects)
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Cấu hình Assignment - Annotator
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Annotator)
                .WithMany(u => u.Assignments)
                .HasForeignKey(a => a.AnnotatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Cấu hình ReviewLog - Reviewer
            modelBuilder.Entity<ReviewLog>()
                .HasOne(r => r.Reviewer)
                .WithMany(u => u.ReviewsGiven)
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Cấu hình UserProjectStat
            modelBuilder.Entity<UserProjectStat>()
                .HasOne(s => s.User)
                .WithMany(u => u.ProjectStats)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 6. Cấu hình Assignment - Project
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Project)
                .WithMany()
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

        }
    }
}