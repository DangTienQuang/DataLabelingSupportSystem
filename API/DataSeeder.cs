using DAL;
using DTOs.Constants;
using DTOs.Entities;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public static class DataSeeder
    {
        /// <summary>
        /// Hàm Seed dữ liệu chính.
        /// </summary>
        /// <param name="serviceProvider">ServiceProvider để tạo scope.</param>
        /// <param name="isDevelopment">Biến xác định môi trường (lấy từ IWebHostEnvironment).</param>
        public static async Task SeedData(IServiceProvider serviceProvider, bool isDevelopment)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await SeedSystemAdmin(context);
                if (isDevelopment)
                {
                    await SeedSampleDataForTesting(context);
                }
            }
        }

        private static async Task SeedSystemAdmin(ApplicationDbContext context)
        {
            if (!await context.Users.AnyAsync(u => u.Role == UserRoles.Admin))
            {
                var admin = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "admin@system.com",
                    FullName = "System Administrator",
                    Role = UserRoles.Admin,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    IsActive = true
                };

                context.Users.Add(admin);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSampleDataForTesting(ApplicationDbContext context)
        {
            if (await context.Projects.AnyAsync()) return;
            var managerId = Guid.NewGuid().ToString();
            var manager = new User
            {
                Id = managerId,
                Email = "manager@test.com",
                FullName = "Project Manager Test",
                Role = UserRoles.Manager,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true
            };
            context.Users.Add(manager);
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
                LabelClasses = new List<LabelClass>
                {
                    new LabelClass { Name = "Object", Color = "#FF0000", GuideLine = "Vẽ khung bao quanh vật thể." }
                }
            };
            context.Projects.Add(project);

            await context.SaveChangesAsync();
        }
    }
}