using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlacementQuizSetController : ControllerBase
    {
        private readonly IPlacementQuizSetService _placementQuizSetService;
        public PlacementQuizSetController(IPlacementQuizSetService placementQuizSetService)
        {
            _placementQuizSetService = placementQuizSetService;
        }
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcelQuizSetFile(IFormFile file, [FromQuery] Guid userId)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var quizSet = await _placementQuizSetService.ImportExcelQuizSetFile(file, userId);
            return Ok(quizSet);
        }
    }
}
