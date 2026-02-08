namespace Core.DTOs.Responses
{
    public class BucketResponse
    {
        public int BucketId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public string Status { get; set; } = "New"; 
    }
}