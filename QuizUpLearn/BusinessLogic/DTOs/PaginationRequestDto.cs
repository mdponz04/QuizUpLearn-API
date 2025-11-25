using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class PaginationRequestDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int Page { get; set; } = 1;
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "asc";
        public Dictionary<string, object>? Filters { get; set; }
        public bool IsValidSortDirection()
        {
            return string.IsNullOrEmpty(SortDirection) || 
                   SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) || 
                   SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        }
        public string GetNormalizedSortDirection()
        {
            if (string.IsNullOrEmpty(SortDirection))
                return "asc";
            
            return SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        }
    }
}
