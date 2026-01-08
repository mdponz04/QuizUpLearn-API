using AutoMapper;
using BusinessLogic.DTOs.RoleDtos;
using BusinessLogic.MappingProfile;
using BusinessLogic.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Entities;
using Repository.Interfaces;

namespace QuizUpLearn.Test.UnitTest
{
    public class RoleServiceTest
    {
        private readonly Mock<IRoleRepo> _mockRoleRepo;
        private readonly IMapper _mapper;
        private readonly RoleService _roleService;

        public RoleServiceTest()
        {
            _mockRoleRepo = new Mock<IRoleRepo>();
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, new NullLoggerFactory());
            _mapper = mapperConfig.CreateMapper();
            _roleService = new RoleService(_mockRoleRepo.Object, _mapper);
        }

        [Fact]
        public async Task CreateRoleAsync_WithValidRequest_ShouldReturnResponseRoleDto()
        {
            var request = new RequestRoleDto
            {
                RoleName = "Admin",
                DisplayName = "Administrator",
                Description = "Admin role",
                Permissions = "All",
                IsActive = true
            };
            var createdRole = new Role
            {
                Id = Guid.NewGuid(),
                RoleName = request.RoleName,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Permissions = request.Permissions ?? "",
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _mockRoleRepo.Setup(r => r.CreateRoleAsync(It.IsAny<Role>())).ReturnsAsync(createdRole);

            var result = await _roleService.CreateRoleAsync(request);

            result.Should().NotBeNull();
            result.Id.Should().Be(createdRole.Id);
            result.RoleName.Should().Be(request.RoleName);
            result.DisplayName.Should().Be(request.DisplayName);
            result.Description.Should().Be(request.Description);
            result.Permissions.Should().Be(request.Permissions);
            result.IsActive.Should().Be(request.IsActive);
        }

        [Fact]
        public async Task GetRoleByIdAsync_WithValidId_ShouldReturnResponseRoleDto()
        {
            var roleId = Guid.NewGuid();
            var role = new Role
            {
                Id = roleId,
                RoleName = "User",
                DisplayName = "User",
                Description = "User role",
                Permissions = "Read",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _mockRoleRepo.Setup(r => r.GetRoleByIdAsync(roleId)).ReturnsAsync(role);

            var result = await _roleService.GetRoleByIdAsync(roleId);

            result.Should().NotBeNull();
            result.Id.Should().Be(roleId);
            result.RoleName.Should().Be(role.RoleName);
        }

        [Fact]
        public async Task GetAllRolesAsync_ShouldReturnListOfResponseRoleDto()
        {
            var roles = new List<Role>
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = "Admin",
                    DisplayName = "Administrator",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    RoleName = "User",
                    DisplayName = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            _mockRoleRepo.Setup(r => r.GetAllRolesAsync(false)).ReturnsAsync(roles);

            var result = await _roleService.GetAllRolesAsync();

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(r => r.RoleName).Should().Contain("Admin").And.Contain("User");
        }

        [Fact]
        public async Task UpdateRoleAsync_WithValidData_ShouldReturnUpdatedResponseRoleDto()
        {
            var roleId = Guid.NewGuid();
            var request = new RequestRoleDto
            {
                RoleName = "Editor",
                DisplayName = "Content Editor",
                Description = "Can edit content",
                Permissions = "Edit",
                IsActive = true
            };
            var updatedRole = new Role
            {
                Id = roleId,
                RoleName = request.RoleName,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Permissions = request.Permissions ?? "",
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };
            _mockRoleRepo.Setup(r => r.GetRoleByIdAsync(roleId)).ReturnsAsync(updatedRole);
            _mockRoleRepo.Setup(r => r.UpdateRoleAsync(roleId, It.IsAny<Role>())).ReturnsAsync(updatedRole);

            var result = await _roleService.UpdateRoleAsync(roleId, request);

            result.Should().NotBeNull();
            result.Id.Should().Be(roleId);
            result.RoleName.Should().Be(request.RoleName);
            result.DisplayName.Should().Be(request.DisplayName);
        }

        [Fact]
        public async Task SoftDeleteRoleAsync_WithValidId_ShouldReturnTrue()
        {
            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, RoleName = "User", DisplayName = "User" };
            _mockRoleRepo.Setup(r => r.GetRoleByIdAsync(roleId)).ReturnsAsync(role);
            _mockRoleRepo.Setup(r => r.SoftDeleteRoleAsync(roleId)).ReturnsAsync(true);

            var result = await _roleService.SoftDeleteRoleAsync(roleId);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task RestoreRoleAsync_WithValidId_ShouldReturnTrue()
        {
            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, RoleName = "User", DisplayName = "User" };
            _mockRoleRepo.Setup(r => r.GetRoleByIdAsync(roleId)).ReturnsAsync(role);
            _mockRoleRepo.Setup(r => r.RestoreRoleAsync(roleId)).ReturnsAsync(true);

            var result = await _roleService.RestoreRoleAsync(roleId);

            result.Should().BeTrue();
        }
    }
}