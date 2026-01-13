using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlacementQuizSetController : ControllerBase
    {
        private readonly IPlacementQuizSetService _placementQuizSetService;

        public PlacementQuizSetController(IPlacementQuizSetService placementQuizSetService)
        {
            _placementQuizSetService = placementQuizSetService;
        }

        [HttpPost("import-excel")]
        [SubscriptionAndRoleAuthorize("Moderator")]
        public async Task<IActionResult> ImportExcelQuizSetFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var userId = (Guid)HttpContext.Items["UserId"]!;

            try
            {
                var quizSet = await _placementQuizSetService.ImportExcelQuizSetFile(file, userId);
                return Ok(quizSet);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
