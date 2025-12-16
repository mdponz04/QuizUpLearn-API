using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizReportDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizReportService : IQuizReportService
    {
        private readonly IQuizReportRepo _quizReportRepo;
        private readonly IMapper _mapper;

        public QuizReportService(IQuizReportRepo quizReportRepo, IMapper mapper)
        {
            _quizReportRepo = quizReportRepo;
            _mapper = mapper;
        }

        public async Task<ResponseQuizReportDto> CreateAsync(RequestQuizReportDto dto)
        {
            var entity = _mapper.Map<QuizReport>(dto);
            var created = await _quizReportRepo.CreateAsync(entity);
            return _mapper.Map<ResponseQuizReportDto>(created);
        }

        public async Task<ResponseQuizReportDto?> GetByIdAsync(Guid id)
        {
            var entity = await _quizReportRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseQuizReportDto>(entity);
        }

        public async Task<PaginationResponseDto<ResponseQuizReportDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizReportRepo.GetAllAsync(includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizReportDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseQuizReportDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizReportRepo.GetByUserIdAsync(userId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizReportDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseQuizReportDto>> GetByQuizIdAsync(Guid quizId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizReportRepo.GetByQuizIdAsync(quizId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizReportDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseQuizReportDto?> GetByUserAndQuizAsync(Guid userId, Guid quizId, bool includeDeleted = false)
        {
            var entity = await _quizReportRepo.GetByUserAndQuizAsync(userId, quizId, includeDeleted);
            return entity == null ? null : _mapper.Map<ResponseQuizReportDto>(entity);
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            return await _quizReportRepo.SoftDeleteAsync(id);
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            return await _quizReportRepo.HardDeleteAsync(id);
        }

        public async Task<bool> IsExistAsync(Guid userId, Guid quizId)
        {
            return await _quizReportRepo.IsExistAsync(userId, quizId);
        }
    }
}
