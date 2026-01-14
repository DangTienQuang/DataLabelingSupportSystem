using Microsoft.EntityFrameworkCore;
using DTOs.Entities;

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
        public DbSet<Annotation> Annotations { get; set; }
        public DbSet<ReviewComment> ReviewComments { get; set; }

        // New Tables
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<WorkflowLog> WorkflowLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- CONFIGURATION ---

            // 1. LabelClass - Annotation (Prevent cascade delete cycle)
            modelBuilder.Entity<Annotation>()
                .HasOne(a => a.LabelClass)
                .WithMany()
                .HasForeignKey(a => a.LabelClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. ProjectMember Configuration
            // Prevent deleting a User from automatically deleting project history in a way that causes cycles
            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Assignment Configuration (Crucial for "User <-> DataItem" link)
            // If you delete a User, their past assignments should probably stay (or be set null), 
            // but Restrict is safest to prevent accidental mass deletion.
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Assignee)
                .WithMany(u => u.Assignments)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.DataItem)
                .WithMany(d => d.Assignments)
                .HasForeignKey(a => a.DataItemId)
                .OnDelete(DeleteBehavior.Cascade); // If DataItem is deleted, assignments go with it.

            // 4. WorkflowLog Configuration
            modelBuilder.Entity<WorkflowLog>()
                .HasOne(w => w.Actor)
                .WithMany()
                .HasForeignKey(w => w.ActorId)
                .OnDelete(DeleteBehavior.Restrict); // Logs should stay even if user is "deleted" (or soft deleted)

            modelBuilder.Entity<WorkflowLog>()
                .HasOne(w => w.DataItem)
                .WithMany(d => d.WorkflowLogs)
                .HasForeignKey(w => w.DataItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}