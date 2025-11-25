using BusinessLogic.DTOs.RoleDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetAllRoles([FromQuery] bool includeDeleted = false)
        {
            var roles = await _roleService.GetAllRolesAsync(includeDeleted);
            return Ok(roles);
        }
        [HttpGet("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            return Ok(role);
        }
        [HttpPost]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> CreateRole([FromBody] RequestRoleDto roleDto)
        {
            var createdRole = await _roleService.CreateRoleAsync(roleDto);
            return CreatedAtAction(nameof(GetRoleById), new { id = createdRole.Id }, createdRole);
        }
        [HttpPut("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RequestRoleDto roleDto)
        {
            var updatedRole = await _roleService.UpdateRoleAsync(id, roleDto);
            return Ok(updatedRole);
        }
        [HttpDelete("{id}")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> SoftDeleteRole(Guid id)
        {
            var result = await _roleService.SoftDeleteRoleAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
        [HttpPost("restore/{id}")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> RestoreRole(Guid id)
        {
            var result = await _roleService.RestoreRoleAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
    }
}
