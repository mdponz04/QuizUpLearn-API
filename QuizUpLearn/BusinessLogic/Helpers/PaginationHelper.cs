using BusinessLogic.DTOs;

namespace BusinessLogic.Helpers
{
    public static class PaginationHelper
    {
        /// <summary>
        /// Creates a simple paginated response from a query
        /// </summary>
        public static async Task<PaginationResponseDto<T>> CreatePagedResponseAsync<T>(
            IQueryable<T> query,
            PaginationRequestDto pagination)
        {
            // Get total count
            var totalCount = await Task.Run(() => query.Count());

            // Apply pagination
            var pagedData = await Task.Run(() => query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList());

            return PaginationResponseDto<T>.Create(pagination, totalCount, pagedData);
        }

        /// <summary>
        /// Creates a simple paginated response from a list
        /// </summary>
        public static PaginationResponseDto<T> CreatePagedResponse<T>(
            IEnumerable<T> source,
            PaginationRequestDto pagination)
        {
            var totalCount = source.Count();
            var pagedData = source
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            return PaginationResponseDto<T>.Create(pagination, totalCount, pagedData);
        }

        /// <summary>
        /// Creates a paginated response with basic sorting
        /// </summary>
        public static async Task<PaginationResponseDto<T>> CreatePagedResponseWithSortingAsync<T>(
            IQueryable<T> query,
            PaginationRequestDto pagination,
            Func<IQueryable<T>, IQueryable<T>>? sortFunc = null)
        {
            // Apply sorting if provided
            if (sortFunc != null)
            {
                query = sortFunc(query);
            }

            // Get total count
            var totalCount = await Task.Run(() => query.Count());

            // Apply pagination
            var pagedData = await Task.Run(() => query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList());

            return PaginationResponseDto<T>.Create(pagination, totalCount, pagedData);
        }

        /// <summary>
        /// Creates a paginated response with search and sorting
        /// </summary>
        public static async Task<PaginationResponseDto<T>> CreatePagedResponseWithSearchAsync<T>(
            IQueryable<T> query,
            PaginationRequestDto pagination,
            Func<IQueryable<T>, string, IQueryable<T>>? searchFunc = null,
            Func<IQueryable<T>, IQueryable<T>>? sortFunc = null)
        {
            // Apply search if provided
            if (!string.IsNullOrEmpty(pagination.SearchTerm) && searchFunc != null)
            {
                query = searchFunc(query, pagination.SearchTerm);
            }

            // Apply sorting if provided
            if (sortFunc != null)
            {
                query = sortFunc(query);
            }

            // Get total count
            var totalCount = await Task.Run(() => query.Count());

            // Apply pagination
            var pagedData = await Task.Run(() => query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList());

            return PaginationResponseDto<T>.Create(pagination, totalCount, pagedData);
        }
    }
}
