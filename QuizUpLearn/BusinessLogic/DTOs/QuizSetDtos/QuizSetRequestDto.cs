using BusinessLogic.DTOs.QuizGroupItemDtos;
using Repository.Enums;
using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs.QuizSetDtos
{
    public class QuizSetRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [EnumDataType(typeof(QuizSetTypeEnum), ErrorMessage = "Invalid QuizSetType value")]
        public QuizSetTypeEnum? QuizSetType { get; set; }
        public Guid? CreatedBy { get; set; }
        public bool? IsPublished { get; set; }
        public bool? IsPremiumOnly { get; set; }
        // navgation property
        public List<RequestQuizGroupItemDto> QuizGroupItems { get; set; } = new List<RequestQuizGroupItemDto>();
    }
}
