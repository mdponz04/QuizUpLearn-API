using AutoMapper;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;
using BusinessLogic.Extensions;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizDtos;

namespace BusinessLogic.Services
{
    public class UserMistakeService : IUserMistakeService
    {
        private readonly IUserMistakeRepo _repo;
        private readonly IUserWeakPointRepo _userWeakPointRepo;
        private readonly IUserWeakPointService _userWeakPointService;
        private readonly IMapper _mapper;

        public UserMistakeService(
            IUserMistakeRepo repo, 
            IUserWeakPointRepo userWeakPointRepo,
            IUserWeakPointService userWeakPointService,
            IMapper mapper)
        {
            _repo = repo;
            _userWeakPointRepo = userWeakPointRepo;
            _userWeakPointService = userWeakPointService;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllAsync(PaginationRequestDto pagination = null!)
        {
            pagination ??= new PaginationRequestDto();
            var userMistakes = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(userMistakes);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseUserMistakeDto?> GetByIdAsync(Guid id)
        {
            var userMistake = await _repo.GetByIdAsync(id);
            return userMistake != null ? _mapper.Map<ResponseUserMistakeDto>(userMistake) : null;
        }

        public async Task AddAsync(RequestUserMistakeDto requestDto)
        {
            var userMistake = _mapper.Map<UserMistake>(requestDto);
            await _repo.AddAsync(userMistake);
        }

        public async Task UpdateAsync(Guid id, RequestUserMistakeDto requestDto)
        {
            await _repo.UpdateAsync(id, _mapper.Map<UserMistake>(requestDto));
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repo.DeleteAsync(id);
        }

        public async Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllByUserIdAsync(Guid userId, PaginationRequestDto pagination = null!)
        {
            pagination ??= new PaginationRequestDto();
            var userMistakes = await _repo.GetAlByUserIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(userMistakes);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<QuizResponseDto>> GetMistakeQuizzesByUserId(Guid userId, PaginationRequestDto pagination)
        {
            // Cleanup WeakPoint orphan (không còn UserMistake liên quan)
            await CleanupOrphanWeakPointsAsync(userId);

            var userMistakes = await _repo.GetAlByUserIdAsync(userId);
            var quizzes = userMistakes
                .Where(um => um.Quiz != null)
                .Select(um => um.Quiz)
                .Distinct()
                .ToList();

            var dtos = _mapper.Map<IEnumerable<QuizResponseDto>>(quizzes);
            return dtos.ToPagedResponse(pagination);
        }

        /// <summary>
        /// Cleanup các UserWeakPoint orphan (không còn UserMistake nào liên quan)
        /// </summary>
        public async Task CleanupOrphanWeakPointsAsync(Guid userId)
        {
            try
            {
                // Lấy tất cả UserWeakPoint của user
                var allWeakPoints = await _userWeakPointRepo.GetByUserIdAsync(userId);
                var weakPointList = allWeakPoints.ToList();

                if (!weakPointList.Any())
                    return;

                // Lấy tất cả UserMistake của user
                var allMistakes = await _repo.GetAlByUserIdAsync(userId);
                var mistakeList = allMistakes.ToList();

                // Tạo set các UserWeakPointId đang được sử dụng bởi UserMistake
                var usedWeakPointIds = new HashSet<Guid>(
                    mistakeList
                        .Where(um => um.UserWeakPointId.HasValue)
                        .Select(um => um.UserWeakPointId!.Value)
                );

                // Tìm và xoá các WeakPoint orphan (không có UserMistake nào trỏ tới)
                var orphanWeakPoints = weakPointList
                    .Where(wp => !usedWeakPointIds.Contains(wp.Id))
                    .ToList();

                foreach (var orphanWeakPoint in orphanWeakPoints)
                {
                    try
                    {
                        await _userWeakPointService.DeleteAsync(orphanWeakPoint.Id);
                    }
                    catch (Exception)
                    {
                        // Bỏ qua lỗi nếu không xoá được
                    }
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi cleanup để không ảnh hưởng flow chính
            }
        }

    }
}
