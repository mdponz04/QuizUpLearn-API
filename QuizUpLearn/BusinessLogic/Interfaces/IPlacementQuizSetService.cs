using BusinessLogic.DTOs.QuizSetDtos;
using Microsoft.AspNetCore.Http;

namespace BusinessLogic.Interfaces
{
    public interface IPlacementQuizSetService
    {
        Task<QuizSetResponseDto> ImportExcelQuizSetFile(IFormFile file, Guid userId);
    }
}
