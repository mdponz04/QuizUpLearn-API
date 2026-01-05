using BusinessLogic.DTOs.BadgeDtos;

namespace BusinessLogic.Interfaces
{
    public interface IBadgeService
    {
        Task<List<ResponseBadgeDto>> GetUserBadgesAsync(Guid userId);
        Task CheckAndAssignBadgesAsync(Guid userId);
    }
}

