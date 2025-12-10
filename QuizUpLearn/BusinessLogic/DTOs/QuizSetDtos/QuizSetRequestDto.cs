using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.QuizGroupItemDtos;
using Repository.Enums;

namespace BusinessLogic.DTOs.QuizSetDtos
{
    public class QuizSetRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QuizSetTypeEnum? QuizSetType { get; set; }
        public string DifficultyLevel { get; set; } = string.Empty;
        public Guid? CreatedBy { get; set; }
        public bool? IsAIGenerated { get; set; }
        public bool? IsPublished { get; set; }
        public bool? IsPremiumOnly { get; set; }
        // navgation property
        public List<RequestQuizGroupItemDto> QuizGroupItems { get; set; } = new List<RequestQuizGroupItemDto>();
    }
}
