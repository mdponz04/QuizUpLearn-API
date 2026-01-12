using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizDtos;
using BusinessLogic.DTOs.UserMistakeDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Helpers;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

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
            ValidateHelper.Validate(pagination);
            
            var userMistakes = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(userMistakes);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseUserMistakeDto?> GetByIdAsync(Guid id)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("User mistake id cannot be empty!");
            var userMistake = await _repo.GetByIdAsync(id);
            return userMistake != null ? _mapper.Map<ResponseUserMistakeDto>(userMistake) : null;
        }

        public async Task AddAsync(RequestUserMistakeDto requestDto)
        {
            if(requestDto == null)
                throw new ArgumentNullException(nameof(requestDto), "Request DTO cannot be null!");
            if(requestDto.UserId == Guid.Empty)
                throw new ArgumentException("User id cannot be empty!");
            if(requestDto.QuizId == Guid.Empty)
                throw new ArgumentException("Quiz id cannot be empty!");

            var userMistake = _mapper.Map<UserMistake>(requestDto);
            await _repo.AddAsync(userMistake);
        }

        public async Task UpdateAsync(Guid id, RequestUserMistakeDto requestDto)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("User mistake id cannot be empty!");
            await _repo.UpdateAsync(id, _mapper.Map<UserMistake>(requestDto));
        }

        public async Task DeleteAsync(Guid id)
        {
            if(id == Guid.Empty)
                throw new ArgumentException("User mistake id cannot be empty!");
            await _repo.DeleteAsync(id);
        }

        public async Task<PaginationResponseDto<ResponseUserMistakeDto>> GetAllByUserIdAsync(Guid userId, PaginationRequestDto pagination = null!)
        {
            if(userId == Guid.Empty)
                throw new ArgumentException("User id cannot be empty!");
            pagination ??= new PaginationRequestDto();
            var userMistakes = await _repo.GetAlByUserIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(userMistakes);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<QuizResponseDto>> GetMistakeQuizzesByUserId(Guid userId, PaginationRequestDto pagination)
        {
            if(userId == Guid.Empty)
                throw new ArgumentException("User id cannot be empty!");
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
            if(userId == Guid.Empty)
                throw new ArgumentException("User id cannot be empty!");
            try
            {
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

        public async Task<IEnumerable<ResponseUserMistakeDto>> GetAllByUserIdAsync(Guid userId)
        {
            return _mapper.Map<IEnumerable<ResponseUserMistakeDto>>(await _repo.GetAlByUserIdAsync(userId));        }
    }
}
