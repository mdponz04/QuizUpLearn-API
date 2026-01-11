using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IAnswerOptionService
    {
        Task<ResponseAnswerOptionDto> CreateAnswerOptionAsync(RequestAnswerOptionDto dto);
        Task<ResponseAnswerOptionDto?> GetAnswerOptionByIdAsync(Guid id);
        Task<IEnumerable<ResponseAnswerOptionDto>> GetAllAnswerOptionAsync(bool includeDeleted = false);
        Task<IEnumerable<ResponseAnswerOptionDto>> GetAnswerOptionByQuizIdAsync(Guid quizId, bool includeDeleted = false);
        Task<ResponseAnswerOptionDto?> UpdateAnswerOptionAsync(Guid id, RequestAnswerOptionDto dto);
        Task<bool> DeleteAnswerOptionAsync(Guid id);
        Task<bool> RestoreAnswerOptionAsync(Guid id);
    }
}
