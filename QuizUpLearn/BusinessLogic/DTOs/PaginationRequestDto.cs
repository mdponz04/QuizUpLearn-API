using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs
{
    public class PaginationRequestDto
    {
        /// <summary>
        /// Page number (1-based)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Search term for filtering results
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Field to sort by
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort direction (asc/desc)
        /// </summary>
        public string? SortDirection { get; set; } = "asc";

        /// <summary>
        /// Additional filters as key-value pairs
        /// </summary>
        public Dictionary<string, object>? Filters { get; set; }

        /// <summary>
        /// Validates sort direction
        /// </summary>
        public bool IsValidSortDirection()
        {
            return string.IsNullOrEmpty(SortDirection) || 
                   SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) || 
                   SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the normalized sort direction
        /// </summary>
        public string GetNormalizedSortDirection()
        {
            if (string.IsNullOrEmpty(SortDirection))
                return "asc";
            
            return SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        }
    }
}
