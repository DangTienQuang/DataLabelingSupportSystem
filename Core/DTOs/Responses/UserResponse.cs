namespace Core.DTOs.Responses
{
    public class UserResponse
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string AvatarUrl { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class PagedResponse<T>
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public object? Stats { get; set; }
        public List<T> Items { get; set; } = new List<T>();
    }
}