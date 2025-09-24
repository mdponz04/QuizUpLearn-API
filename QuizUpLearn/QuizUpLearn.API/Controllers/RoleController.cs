using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles([FromQuery] bool includeDeleted = false)
        {
            var roles = await _roleService.GetAllRolesAsync(includeDeleted);
            return Ok(roles);
        }
    }
}
