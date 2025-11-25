using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _uploadService;

        public UploadController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }
        /// <summary>
        /// Uploads a file and returns its URL & public ID.
        /// </summary>
        /// <param name="file">The file that need to be uploaded</param>
        /// <param name="publicId">If the file that already uploaded then should put public id to trigger replace</param>
        /// <returns></returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, string? publicId = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            var result = await _uploadService.UploadAsync(file, publicId);
            if(string.IsNullOrEmpty(result.Url))
                return StatusCode(StatusCodes.Status500InternalServerError, "File upload failed.");
            return Ok(new { fileUrl = result.Url, publicId = result.PublicId });
        }
        /// <summary>
        /// Delete a file by its URL and optional public ID.
        /// </summary>
        /// <param name="fileUrl"></param>
        /// <param name="publicId"></param>
        /// <returns></returns>
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] string fileUrl, [FromQuery] string? publicId = null)
        {
            var success = await _uploadService.DeleteFileAsync(fileUrl, publicId);

            if (!success)
                return NotFound("File not found or failed to delete");

            return Ok("Deleted successfully");
        }
    }
}
