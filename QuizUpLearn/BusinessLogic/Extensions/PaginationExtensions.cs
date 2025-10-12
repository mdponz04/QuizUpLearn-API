using BusinessLogic.DTOs;

namespace BusinessLogic.Extensions
{
    public static class PaginationExtensions
    {
        /// <summary>
        /// Applies pagination to an IQueryable
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PaginationRequestDto pagination)
        {
            return query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize);
        }

        /// <summary>
        /// Applies sorting to an IQueryable based on property name (simplified)
        /// </summary>
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, PaginationRequestDto pagination)
        {
            if (string.IsNullOrEmpty(pagination.SortBy))
                return query;

            var property = typeof(T).GetProperty(pagination.SortBy, 
                System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (property == null)
                return query;

            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
            var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, property);
            var lambda = System.Linq.Expressions.Expression.Lambda(propertyAccess, parameter);

            var methodName = pagination.GetNormalizedSortDirection() == "desc" ? "OrderByDescending" : "OrderBy";
            var resultExpression = System.Linq.Expressions.Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), property.PropertyType },
                query.Expression,
                System.Linq.Expressions.Expression.Quote(lambda));

            return query.Provider.CreateQuery<T>(resultExpression);
        }

        /// <summary>
        /// Applies search filtering to an IQueryable for string properties (simplified)
        /// </summary>
        public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, PaginationRequestDto pagination)
        {
            if (string.IsNullOrEmpty(pagination.SearchTerm))
                return query;

            var searchTerm = pagination.SearchTerm.ToLower();
            var stringProperties = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(string))
                .ToArray();

            if (!stringProperties.Any())
                return query;

            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
            System.Linq.Expressions.Expression? combinedExpression = null;

            foreach (var property in stringProperties)
            {
                var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, property);
                var nullCheck = System.Linq.Expressions.Expression.NotEqual(propertyAccess, 
                    System.Linq.Expressions.Expression.Constant(null, typeof(string)));
                
                var toLower = System.Linq.Expressions.Expression.Call(propertyAccess, 
                    typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                
                var contains = System.Linq.Expressions.Expression.Call(toLower, 
                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!, 
                    System.Linq.Expressions.Expression.Constant(searchTerm));

                var propertyExpression = System.Linq.Expressions.Expression.AndAlso(nullCheck, contains);

                if (combinedExpression == null)
                    combinedExpression = propertyExpression;
                else
                    combinedExpression = System.Linq.Expressions.Expression.OrElse(combinedExpression, propertyExpression);
            }

            if (combinedExpression != null)
            {
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        /// <summary>
        /// Creates a paginated response from a query (simplified)
        /// </summary>
        public static async Task<PaginationResponseDto<T>> ToPagedResponseAsync<T>(
            this IQueryable<T> query, 
            PaginationRequestDto pagination)
        {
            // Get total count before pagination
            var totalCount = await Task.Run(() => query.Count());

            // Apply sorting, search, and pagination
            var pagedQuery = query
                .ApplySorting(pagination)
                .ApplySearch(pagination)
                .ApplyPagination(pagination);

            // Execute the query
            var data = await Task.Run(() => pagedQuery.ToList());

            return PaginationResponseDto<T>.Create(pagination, totalCount, data);
        }

        /// <summary>
        /// Creates a paginated response from a list (simplified)
        /// </summary>
        public static PaginationResponseDto<T> ToPagedResponse<T>(
            this IEnumerable<T> source, 
            PaginationRequestDto pagination)
        {
            var query = source.AsQueryable();
            return query.ToPagedResponseAsync(pagination).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Simple pagination helper for basic scenarios
        /// </summary>
        public static (List<T> Data, int TotalCount) GetPagedData<T>(
            this IQueryable<T> query, 
            int page, 
            int pageSize)
        {
            var totalCount = query.Count();
            var data = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (data, totalCount);
        }
    }
}
