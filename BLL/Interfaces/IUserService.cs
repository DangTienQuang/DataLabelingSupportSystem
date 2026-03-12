using Core.DTOs.Requests;
using Core.DTOs.Responses;
using Core.Entities;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces
{
    public interface IUserService
    {
        Task<User> RegisterAsync(string fullName, string email, string password, string role);
        Task<string?> LoginAsync(string email, string password);
        Task<User?> GetUserByIdAsync(string id);
        Task<bool> IsEmailExistsAsync(string email);
        Task ChangePasswordAsync(string userId, string oldPassword, string newPassword);
        Task UpdateProfileAsync(string userId, UpdateProfileRequest request);
        Task<PagedResponse<UserResponse>> GetAllUsersAsync(int page, int pageSize);
        Task UpdateUserAsync(string userId, UpdateUserRequest request);
        Task DeleteUserAsync(string userId);
        Task ToggleUserStatusAsync(string userId, bool isActive);
        Task UpdateAvatarAsync(string userId, string avatarUrl);
        Task<ImportUserResponse> ImportUsersFromExcelAsync(IFormFile file, string adminId);
    }
}