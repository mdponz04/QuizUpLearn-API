using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.DTOs.QuizQuizSetDtos
{
    public class RequestQuizQuizSetDto
    {
        [Required(ErrorMessage = "Quiz ID is required")]
        public Guid QuizId { get; set; }
        
        [Required(ErrorMessage = "Quiz Set ID is required")]
        public Guid QuizSetId { get; set; }
    }
}