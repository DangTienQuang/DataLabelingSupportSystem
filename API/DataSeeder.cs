using DAL;
using DTOs.Constants;
using DTOs.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace API
{
    public static class DataSeeder
    {
        public static async Task SeedData(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // --- 1. SEED USERS (ĐỦ 4 ROLES: Admin, Manager, Reviewer, Annotator) ---

                // Mật khẩu chung: 123456
                var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

                // A. TẠO ADMIN (Quản trị hệ thống)
                var adminId = "00000000-0000-0000-0000-000000000000"; // ID Vip
                if (!await context.Users.AnyAsync(u => u.Email == "Admin@gmail.com"))
                {
                    context.Users.Add(new User
                    {
                        Id = adminId,
                        Email = "Admin@gmail.com",
                        FullName = "System Administrator",
                        Role = "Admin", // Role Admin
                        PasswordHash = defaultPasswordHash,
                        IsActive = true
                    });
                }

                // B. TẠO MANAGER (Quản lý dự án)
                var managerId = "11111111-1111-1111-1111-111111111111";
                if (!await context.Users.AnyAsync(u => u.Email == "Manager@gmail.com"))
                {
                    context.Users.Add(new User
                    {
                        Id = managerId,
                        Email = "Manager@gmail.com",
                        FullName = "Project Manager",
                        Role = "Manager", // Role Manager
                        PasswordHash = defaultPasswordHash,
                        IsActive = true
                    });
                }

                // C. TẠO REVIEWER (Người kiểm duyệt)
                var reviewerId = "33333333-3333-3333-3333-333333333333";
                if (!await context.Users.AnyAsync(u => u.Email == "Reviewer@gmail.com"))
                {
                    context.Users.Add(new User
                    {
                        Id = reviewerId,
                        Email = "Reviewer@gmail.com",
                        FullName = "Senior Reviewer",
                        Role = "Reviewer", // Role Reviewer
                        PasswordHash = defaultPasswordHash,
                        IsActive = true
                    });
                }

                // D. TẠO ANNOTATORS (Nhân viên gán nhãn)
                var annotators = new List<User>();
                for (int i = 1; i <= 5; i++)
                {
                    var staffId = $"22222222-2222-2222-2222-22222222222{i}";
                    var email = $"Staff{i}@gmail.com";

                    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (user == null)
                    {
                        user = new User
                        {
                            Id = staffId,
                            Email = email,
                            FullName = $"Staff Annotator {i}",
                            Role = "Annotator", // Role Annotator
                            PasswordHash = defaultPasswordHash,
                            IsActive = true
                        };
                        context.Users.Add(user);
                        annotators.Add(user);
                    }
                    else
                    {
                        annotators.Add(user);
                    }
                }

                await context.SaveChangesAsync();

                // --- 2. SEED PROJECTS & DATA ---
                if (!context.Projects.Any())
                {
                    var projects = new List<Project>();

                    for (int p = 1; p <= 3; p++)
                    {
                        var project = new Project
                        {
                            Name = $"Project {p}: Nhận diện xe cộ",
                            Description = "Dự án mẫu để test luồng Annotation & Review.",
                            ManagerId = managerId, // Manager quản lý
                            CreatedDate = DateTime.UtcNow,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddDays(30),
                            PricePerLabel = 1000,
                            TotalBudget = 1000000,
                            Deadline = DateTime.UtcNow.AddDays(10),
                            AllowGeometryTypes = "Rectangle"
                        };

                        // Label Classes
                        project.LabelClasses = new List<LabelClass>
                        {
                            new LabelClass { Name = "Car", Color = "#FF0000" },
                            new LabelClass { Name = "Bike", Color = "#00FF00" }
                        };

                        // Tạo 10 ảnh
                        var dataItems = new List<DataItem>();
                        for (int d = 1; d <= 10; d++)
                        {
                            dataItems.Add(new DataItem
                            {
                                StorageUrl = $"https://via.placeholder.com/600x400?text=Img_{d}",
                                Status = "New",
                                UploadedDate = DateTime.UtcNow
                            });
                        }

                        // Giao việc & Tạo Review Log
                        int staffIndex = 0;
                        for (int k = 0; k < 8; k++)
                        {
                            var item = dataItems[k];
                            // Logic trạng thái để test thanh tiến độ
                            var status = "Assigned";
                            if (k == 0 || k == 1) status = "Approved";  // Đã xong
                            else if (k == 2 || k == 3) status = "Submitted"; // Chờ review
                            else if (k == 4) status = "Rejected";   // Bị từ chối
                            else if (k == 5) status = "InProgress"; // Đang làm

                            item.Status = status;

                            var assignedStaff = annotators[staffIndex % annotators.Count];

                            var assignment = new Assignment
                            {
                                Project = project,
                                DataItem = item,
                                AnnotatorId = assignedStaff.Id,
                                Status = status,
                                AssignedDate = DateTime.UtcNow,
                                SubmittedAt = (status != "Assigned" && status != "InProgress") ? DateTime.UtcNow : null
                            };

                            // Nếu status là Approved hoặc Rejected thì phải có ReviewLog của Reviewer
                            if (status == "Rejected" || status == "Approved")
                            {
                                assignment.ReviewLogs = new List<ReviewLog>
                                {
                                    new ReviewLog
                                    {
                                        ReviewerId = reviewerId, // <--- Reviewer ID cố định duyệt bài
                                        Verdict = status,
                                        ErrorCategory = status == "Rejected" ? ErrorCategories.IncorrectLabel : null,
                                        Comment = status == "Rejected" ? "Vẽ sai box rồi bạn ơi." : "Làm tốt lắm.",
                                        CreatedAt = DateTime.UtcNow
                                    }
                                };
                            }

                            if (item.Assignments == null) item.Assignments = new List<Assignment>();
                            item.Assignments.Add(assignment);
                            staffIndex++;
                        }

                        project.DataItems = dataItems;
                        projects.Add(project);
                    }

                    context.Projects.AddRange(projects);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}