namespace BusinessLogic.DTOs
{
    public class PaginationResponseDto<T>
    {
        /// <summary>
        /// The paginated data items
        /// </summary>
        public List<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Search term used for filtering
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Field used for sorting
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort direction used
        /// </summary>
        public string? SortDirection { get; set; }

        /// <summary>
        /// Creates a pagination response from the request and total count
        /// </summary>
        public static PaginationResponseDto<T> Create(PaginationRequestDto request, int totalCount, List<T> data)
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            
            return new PaginationResponseDto<T>
            {
                Data = data,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = request.Page > 1,
                HasNextPage = request.Page < totalPages,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortDirection = request.GetNormalizedSortDirection()
            };
        }
    }
}
