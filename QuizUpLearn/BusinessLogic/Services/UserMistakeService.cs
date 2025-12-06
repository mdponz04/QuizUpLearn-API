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
            await CleanupOrphanWeakPointsAsync(userId);

            var userMistakes = await _repo.GetAlByUserIdAsync(userId);
            var mistakeList = userMistakes.ToList();

            // Kiểm tra xem có UserMistake nào chưa được AI phân tích không
            // Phải đợi TẤT CẢ câu đều được phân tích mới được làm lại
            var unanalyzedMistakes = mistakeList.Where(um => !um.IsAnalyzed).ToList();
            if (unanalyzedMistakes.Any())
            {
                throw new InvalidOperationException(
                    $"Vui lòng đợi AI phân tích xong TẤT CẢ các câu sai trước khi làm lại. " +
                    $"Còn {unanalyzedMistakes.Count} câu chưa được phân tích.");
            }

            // Chỉ lấy các quiz từ UserMistake đã được phân tích (tất cả đều IsAnalyzed = true)
            var quizzes = mistakeList
                .Where(um => um.Quiz != null)
                .Select(um => um.Quiz)
                .Distinct()
                .ToList();

            var dtos = _mapper.Map<IEnumerable<QuizResponseDto>>(quizzes);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task CleanupOrphanWeakPointsAsync(Guid userId)
        {
            try
            {
                var allWeakPoints = await _userWeakPointRepo.GetByUserIdAsync(userId);
                var weakPointList = allWeakPoints.ToList();

                if (!weakPointList.Any())
                    return;

                foreach (var wp in weakPointList)
                {
                    if(wp.UserMistakes == null || wp.UserMistakes.Count == 0)
                    {
                        await _userWeakPointService.DeleteAsync(wp.Id);
                    }
                    try
                    {
                        await _userWeakPointService.DeleteAsync(wp.Id);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public async Task<IEnumerable<ResponseUserMistakeDto>> GetAllByUserIdAsync(Guid userId)
        {
            return _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(await _repo.GetAlByUserIdAsync(userId));        }
    }
}
