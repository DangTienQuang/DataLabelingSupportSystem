using DAL;
using Core.Constants;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public static class DataSeeder
    {
        public static async Task SeedData(IServiceProvider serviceProvider, bool isDevelopment)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (isDevelopment)
                {
                    await SeedSampleDataForTesting(context);
                }
            }
        }

        private static async Task SeedSampleDataForTesting(ApplicationDbContext context)
        {
            if (await context.Users.AnyAsync()) return;

            var adminId = Guid.NewGuid().ToString();
            var managerId = Guid.NewGuid().ToString();
            var annotatorId = Guid.NewGuid().ToString();
            var reviewerId = Guid.NewGuid().ToString();

            var users = new List<User>
            {
                new User { Id = adminId, FullName = "Admin User", Email = "admin@system.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), Role = UserRoles.Admin, IsActive = true },
                new User { Id = managerId, FullName = "Manager User", Email = "manager@system.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), Role = UserRoles.Manager, IsActive = true },
                new User { Id = annotatorId, FullName = "Annotator User", Email = "annotator@system.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), Role = UserRoles.Annotator, IsActive = true },
                new User { Id = reviewerId, FullName = "Reviewer User", Email = "reviewer@system.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), Role = UserRoles.Reviewer, IsActive = true }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            var project = new Project
            {
                Name = "Dự án Thử nghiệm (Dev Only)",
                Description = "Dữ liệu này chỉ xuất hiện trong môi trường Development.",
                ManagerId = managerId,
                PricePerLabel = 1000,
                TotalBudget = 1000000,
                Deadline = DateTime.UtcNow.AddDays(30),
                CreatedDate = DateTime.UtcNow,
                AllowGeometryTypes = "Rectangle",
                AnnotationGuide = "Đây là hướng dẫn gán nhãn mẫu cho dự án thử nghiệm.",
                PenaltyUnit = 10,
                LabelClasses = new List<LabelClass>
                {
                    new LabelClass { Name = "Object", Color = "#FF0000", GuideLine = "Vẽ khung bao quanh vật thể." }
                }
            };

            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
        }
    }
}