using AutoMapper;
using BusinessLogic.DTOs.BadgeDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;
using QuizUpLearn.API.Models;
using Repository.Interfaces;
using System.Net;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BadgeController : ControllerBase
    {
        private readonly IBadgeRepo _badgeRepo;
        private readonly IUserBadgeRepo _userBadgeRepo;
        private readonly IUserRepo _userRepo;
        private readonly IBadgeService _badgeService;
        private readonly IMapper _mapper;

        public BadgeController(
            IBadgeRepo badgeRepo,
            IUserBadgeRepo userBadgeRepo,
            IUserRepo userRepo,
            IBadgeService badgeService,
            IMapper mapper)
        {
            _badgeRepo = badgeRepo;
            _userBadgeRepo = userBadgeRepo;
            _userRepo = userRepo;
            _badgeService = badgeService;
            _mapper = mapper;
        }

        /// <summary>
        /// Lấy danh sách tất cả badge
        /// </summary>
        /// <param name="includeDeleted">Có bao gồm badge đã xóa không</param>
        /// <returns>Danh sách badge</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ResponseBadgeDto>>>> GetAllBadges([FromQuery] bool includeDeleted = false)
        {
            try
            {
                var badges = await _badgeRepo.GetAllAsync(includeDeleted);
                var responseDtos = _mapper.Map<List<ResponseBadgeDto>>(badges);

                return Ok(new ApiResponse<List<ResponseBadgeDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách badge thành công",
                    Data = responseDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ResponseBadgeDto>>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Lấy badge theo ID
        /// </summary>
        /// <param name="id">Badge ID</param>
        /// <returns>Badge</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ResponseBadgeDto>>> GetBadgeById([FromRoute] Guid id)
        {
            try
            {
                var badge = await _badgeRepo.GetByIdAsync(id);
                if (badge == null)
                {
                    return NotFound(new ApiResponse<ResponseBadgeDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy badge"
                    });
                }

                var responseDto = _mapper.Map<ResponseBadgeDto>(badge);

                return Ok(new ApiResponse<ResponseBadgeDto>
                {
                    Success = true,
                    Message = "Lấy badge thành công",
                    Data = responseDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ResponseBadgeDto>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Lấy badge theo Code
        /// </summary>
        /// <param name="code">Badge Code</param>
        /// <returns>Badge</returns>
        [HttpGet("code/{code}")]
        public async Task<ActionResult<ApiResponse<ResponseBadgeDto>>> GetBadgeByCode([FromRoute] string code)
        {
            try
            {
                var badge = await _badgeRepo.GetByCodeAsync(code);
                if (badge == null)
                {
                    return NotFound(new ApiResponse<ResponseBadgeDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy badge"
                    });
                }

                var responseDto = _mapper.Map<ResponseBadgeDto>(badge);

                return Ok(new ApiResponse<ResponseBadgeDto>
                {
                    Success = true,
                    Message = "Lấy badge thành công",
                    Data = responseDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ResponseBadgeDto>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Tạo badge mới
        /// </summary>
        /// <param name="dto">Thông tin badge</param>
        /// <returns>Badge đã tạo</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ResponseBadgeDto>>> CreateBadge([FromBody] RequestBadgeDto dto)
        {
            try
            {
                // Kiểm tra Code đã tồn tại chưa (nếu có Code)
                if (!string.IsNullOrEmpty(dto.Code))
                {
                    var existingBadge = await _badgeRepo.GetByCodeAsync(dto.Code);
                    if (existingBadge != null)
                    {
                        return BadRequest(new ApiResponse<ResponseBadgeDto>
                        {
                            Success = false,
                            Message = $"Badge với Code '{dto.Code}' đã tồn tại"
                        });
                    }
                }

                // Map DTO sang Entity
                var badgeEntity = _mapper.Map<Repository.Entities.Badge>(dto);
                
                // Tạo badge
                var created = await _badgeRepo.CreateAsync(badgeEntity);
                var responseDto = _mapper.Map<ResponseBadgeDto>(created);

                return Ok(new ApiResponse<ResponseBadgeDto>
                {
                    Success = true,
                    Message = "Tạo badge thành công",
                    Data = responseDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ResponseBadgeDto>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Tạo nhiều badge cùng lúc
        /// </summary>
        /// <param name="dtos">Danh sách badge cần tạo</param>
        /// <returns>Danh sách badge đã tạo</returns>
        [HttpPost("bulk")]
        public async Task<ActionResult<ApiResponse<List<ResponseBadgeDto>>>> CreateBulkBadges([FromBody] List<RequestBadgeDto> dtos)
        {
            try
            {
                var createdBadges = new List<ResponseBadgeDto>();
                var errors = new List<string>();

                foreach (var dto in dtos)
                {
                    try
                    {
                        // Kiểm tra Code đã tồn tại chưa (nếu có Code)
                        if (!string.IsNullOrEmpty(dto.Code))
                        {
                            var existingBadge = await _badgeRepo.GetByCodeAsync(dto.Code);
                            if (existingBadge != null)
                            {
                                errors.Add($"Badge với Code '{dto.Code}' đã tồn tại");
                                continue;
                            }
                        }

                        // Map DTO sang Entity
                        var badgeEntity = _mapper.Map<Repository.Entities.Badge>(dto);
                        
                        // Tạo badge
                        var created = await _badgeRepo.CreateAsync(badgeEntity);
                        var responseDto = _mapper.Map<ResponseBadgeDto>(created);
                        createdBadges.Add(responseDto);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi khi tạo badge '{dto.Name}': {ex.Message}");
                    }
                }

                if (createdBadges.Count == 0)
                {
                    return BadRequest(new ApiResponse<List<ResponseBadgeDto>>
                    {
                        Success = false,
                        Message = "Không tạo được badge nào",
                        Error = string.Join("; ", errors)
                    });
                }

                var message = $"Đã tạo {createdBadges.Count}/{dtos.Count} badge thành công";
                if (errors.Any())
                {
                    message += $". Lỗi: {string.Join("; ", errors)}";
                }

                return Ok(new ApiResponse<List<ResponseBadgeDto>>
                {
                    Success = true,
                    Message = message,
                    Data = createdBadges
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ResponseBadgeDto>>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gán badge cho user
        /// </summary>
        /// <param name="dto">Thông tin UserId và BadgeId hoặc BadgeCode</param>
        /// <returns>Kết quả gán badge</returns>
        [HttpPost("assign")]
        public async Task<ActionResult<ApiResponse<ResponseUserBadgeDto>>> AssignBadge([FromBody] RequestUserBadgeDto dto)
        {
            try
            {
                // Kiểm tra user tồn tại
                var user = await _userRepo.GetByIdAsync(dto.UserId);
                if (user == null)
                {
                    return BadRequest(new ApiResponse<ResponseUserBadgeDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy user"
                    });
                }

                // Lấy badge theo BadgeId hoặc BadgeCode
                Repository.Entities.Badge? badge = null;
                if (dto.BadgeId.HasValue)
                {
                    badge = await _badgeRepo.GetByIdAsync(dto.BadgeId.Value);
                }
                else if (!string.IsNullOrEmpty(dto.BadgeCode))
                {
                    badge = await _badgeRepo.GetByCodeAsync(dto.BadgeCode);
                }

                if (badge == null)
                {
                    return BadRequest(new ApiResponse<ResponseUserBadgeDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy badge"
                    });
                }

                // Kiểm tra user đã có badge này chưa
                var exists = await _userBadgeRepo.ExistsAsync(dto.UserId, badge.Id);
                if (exists)
                {
                    return BadRequest(new ApiResponse<ResponseUserBadgeDto>
                    {
                        Success = false,
                        Message = "User đã có badge này rồi"
                    });
                }

                // Tạo UserBadge
                var userBadgeEntity = new Repository.Entities.UserBadge
                {
                    UserId = dto.UserId,
                    BadgeId = badge.Id
                };

                var created = await _userBadgeRepo.CreateAsync(userBadgeEntity);
                var responseDto = _mapper.Map<ResponseUserBadgeDto>(created);
                responseDto.Badge = _mapper.Map<ResponseBadgeDto>(badge);

                return Ok(new ApiResponse<ResponseUserBadgeDto>
                {
                    Success = true,
                    Message = "Gán badge thành công",
                    Data = responseDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ResponseUserBadgeDto>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Check và assign badges cho user (dùng cho user cũ hoặc re-check)
        /// </summary>
        /// <param name="userId">User ID cần check badges</param>
        /// <returns>Kết quả check badges</returns>
        [HttpPost("check/{userId}")]
        public async Task<ActionResult<ApiResponse<object>>> CheckBadgesForUser([FromRoute] Guid userId)
        {
            try
            {
                // Kiểm tra user tồn tại
                var user = await _userRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy user"
                    });
                }

                // Check và assign badges
                await _badgeService.CheckAndAssignBadgesAsync(userId);
                
                // Lấy badges sau khi check
                var badges = await _badgeService.GetUserBadgesAsync(userId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Đã cập nhật badges thành công. User có {badges.Count} badge(s).",
                    Data = new
                    {
                        UserId = userId,
                        BadgeCount = badges.Count,
                        Badges = badges
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Check badges cho tất cả users (Admin only - chạy batch)
        /// </summary>
        /// <param name="batchSize">Số lượng users xử lý mỗi lần (mặc định: 50)</param>
        /// <returns>Kết quả batch process</returns>
        [HttpPost("check-all")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<ActionResult<ApiResponse<object>>> CheckBadgesForAllUsers([FromQuery] int batchSize = 50)
        {
            try
            {
                // Lấy tất cả users
                var allUsers = await _userRepo.GetAllAsync(includeDeleted: false);
                var usersList = allUsers.ToList();
                var totalUsers = usersList.Count;
                var processed = 0;
                var errors = 0;
                var errorDetails = new List<string>();

                // Chạy batch process
                for (int i = 0; i < usersList.Count; i += batchSize)
                {
                    var batch = usersList.Skip(i).Take(batchSize).ToList();
                    
                    foreach (var user in batch)
                    {
                        try
                        {
                            await _badgeService.CheckAndAssignBadgesAsync(user.Id);
                            processed++;
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            errorDetails.Add($"User {user.Id}: {ex.Message}");
                        }
                    }

                    // Delay giữa các batch để tránh overload DB
                    if (i + batchSize < usersList.Count)
                    {
                        await Task.Delay(500); // Delay ngắn hơn vì chạy sync
                    }
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Đã cập nhật badges cho {processed}/{totalUsers} users thành công.",
                    Data = new
                    {
                        TotalUsers = totalUsers,
                        Processed = processed,
                        Errors = errors,
                        ErrorDetails = errorDetails.Any() ? errorDetails : null
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                });
            }
        }
    }
}

