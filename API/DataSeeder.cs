using DAL;
using DTOs.Constants;
using DTOs.Entities;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public static class DataSeeder
    {
        public static async Task SeedData(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // --- 1. SEED USERS ---
                var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

                // A. TẠO ADMIN
                if (!await context.Users.AnyAsync(u => u.Email == "Admin@gmail.com"))
                {
                    context.Users.Add(new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = "Admin@gmail.com",
                        FullName = "System Administrator",
                        Role = "Admin",
                        PasswordHash = defaultPasswordHash,
                        IsActive = true
                    });
                }

                // B. TẠO MANAGER
                var managerId = "11111111-1111-1111-1111-111111111111";
                if (!await context.Users.AnyAsync(u => u.Email == "Manager@gmail.com"))
                {
                    context.Users.Add(new User
                    {
                        Id = managerId,
                        Email = "Manager@gmail.com",
                        FullName = "Project Manager",
                        Role = "Manager",
                        PasswordHash = defaultPasswordHash,
                        IsActive = true
                    });
                }

                // C. TẠO REVIEWER
                var reviewerId = "33333333-3333-3333-3333-333333333333";
                if (!await context.Users.AnyAsync(u => u.Email == "Reviewer@gmail.com"))
                {
                    context.Users.Add(new User
                    {
                        Id = reviewerId,
                        Email = "Reviewer@gmail.com",
                        FullName = "Senior Reviewer",
                        Role = "Reviewer",
                        PasswordHash = defaultPasswordHash,
                        IsActive = true
                    });
                }

                // D. TẠO ANNOTATORS
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
                            Role = "Annotator",
                            PasswordHash = defaultPasswordHash,
                            IsActive = true
                        };
                        context.Users.Add(user);
                    }
                    annotators.Add(user);
                }

                await context.SaveChangesAsync();

                // --- 2. SEED PROJECTS & DATA ---
                if (!context.Projects.Any())
                {
                    var project = new Project
                    {
                        Name = "Project 1: Nhận diện xe cộ (Vehicle Detection)",
                        Description = "Dự án gán nhãn xe ô tô và xe máy cho hệ thống AI giao thông.",
                        ManagerId = managerId,
                        CreatedDate = DateTime.UtcNow,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(30),
                        PricePerLabel = 2000,
                        TotalBudget = 5000000,
                        Deadline = DateTime.UtcNow.AddDays(15),
                        AllowGeometryTypes = "Rectangle"
                    };

                    // Label Classes + Guideline Mới
                    project.LabelClasses = new List<LabelClass>
                    {
                        new LabelClass
                        {
                            Name = "Car",
                            Color = "#FF0000",
                            GuideLine = "Vẽ khung chữ nhật (Box) bao quanh toàn bộ xe ô tô. Bao gồm: Sedan, SUV, Xe tải nhỏ. KHÔNG vẽ xe buýt hoặc xe container."
                        },
                        new LabelClass
                        {
                            Name = "Bike",
                            Color = "#00FF00",
                            GuideLine = "Vẽ bao gồm cả người lái và xe máy. Nếu xe bị che khuất quá 50% thì bỏ qua."
                        }
                    };

                    // Tạo 10 DataItems (Ảnh)
                    var dataItems = new List<DataItem>();
                    for (int d = 1; d <= 10; d++)
                    {
                        dataItems.Add(new DataItem
                        {
                            StorageUrl = $"https://via.placeholder.com/800x600?text=Vehicle_Image_{d}",
                            Status = "New",
                            UploadedDate = DateTime.UtcNow
                        });
                    }

                    // Giao việc & Tạo Review Log
                    int staffIndex = 0;
                    var assignments = new List<Assignment>();

                    for (int k = 0; k < 8; k++) // Assign 8 ảnh đầu
                    {
                        var item = dataItems[k];
                        var assignedStaff = annotators[staffIndex % annotators.Count];

                        // Set trạng thái giả lập
                        string status = "Assigned";
                        if (k == 0 || k == 1) status = "Completed";   // Đã duyệt (Approved -> Completed)
                        else if (k == 2) status = "Rejected";         // Bị từ chối
                        else if (k == 3) status = "Submitted";        // Đang chờ duyệt

                        item.Status = status;

                        var assignment = new Assignment
                        {
                            Project = project,
                            DataItem = item,
                            AnnotatorId = assignedStaff.Id,
                            Status = status,
                            AssignedDate = DateTime.UtcNow,
                            SubmittedAt = (status != "Assigned") ? DateTime.UtcNow : null
                        };

                        // Tạo Log Review nếu đã từng được chấm
                        if (status == "Completed" || status == "Rejected")
                        {
                            assignment.ReviewLogs = new List<ReviewLog>
                            {
                                new ReviewLog
                                {
                                    ReviewerId = reviewerId,
                                    Verdict = status == "Completed" ? "Approved" : "Rejected",
                                    // Sử dụng mã lỗi chuẩn mới (Updated)
                                    ErrorCategory = status == "Rejected" ? ErrorCategories.TE01_WrongBox : null,
                                    Comment = status == "Rejected" ? "Box vẽ quá rộng, dư nhiều nền đường." : "Vẽ tốt, đúng guideline.",
                                    CreatedAt = DateTime.UtcNow,
                                    // Giả lập điểm phạt
                                    ScorePenalty = status == "Rejected" ? 10 : 0
                                }
                            };
                        }

                        assignments.Add(assignment);
                        staffIndex++;
                    }

                    if (project.DataItems == null) project.DataItems = new List<DataItem>();
                    ((List<DataItem>)project.DataItems).AddRange(dataItems);

                    // Add Assignments thủ công vào context sau khi project được add
                    // (EF Core sẽ tự mapping nếu add Project, nhưng ở đây ta add Assignment vào item)
                    foreach (var assign in assignments)
                    {
                        var item = dataItems.First(d => d == assign.DataItem);
                        if (item.Assignments == null) item.Assignments = new List<Assignment>();
                        item.Assignments.Add(assign);
                    }

                    context.Projects.Add(project);
                    await context.SaveChangesAsync();

                    // --- 3. SEED STATS (BẢNG ĐIỂM) ---
                    // Phải có cái này thì Dashboard mới hiện số liệu ngay lập tức

                    // A. Stats cho Annotators
                    foreach (var staff in annotators)
                    {
                        var staffAssignments = assignments.Where(a => a.AnnotatorId == staff.Id).ToList();
                        if (staffAssignments.Any())
                        {
                            int approved = staffAssignments.Count(a => a.Status == "Completed");
                            int rejected = staffAssignments.Count(a => a.Status == "Rejected");
                            int total = staffAssignments.Count;

                            // Tính điểm giả lập
                            double avgQuality = 100;
                            if (rejected > 0) avgQuality = 80; // Giả sử bị trừ điểm

                            context.UserProjectStats.Add(new UserProjectStat
                            {
                                UserId = staff.Id,
                                ProjectId = project.Id,
                                TotalAssigned = total,
                                TotalApproved = approved,
                                TotalRejected = rejected,
                                EfficiencyScore = total > 0 ? ((float)approved / total) * 100 : 0,
                                AverageQualityScore = avgQuality, // KPI mới
                                EstimatedEarnings = approved * project.PricePerLabel,
                                Date = DateTime.UtcNow
                            });
                        }
                    }

                    // B. Stats cho Reviewer (KPI Reviewer)
                    context.UserProjectStats.Add(new UserProjectStat
                    {
                        UserId = reviewerId,
                        ProjectId = project.Id,
                        ReviewerQualityScore = 95.5, // Giả lập điểm cao
                        TotalReviewsDone = 3,        // Đã chấm 3 bài
                        TotalAuditedReviews = 1,
                        TotalCorrectDecisions = 1,
                        Date = DateTime.UtcNow
                    });

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}