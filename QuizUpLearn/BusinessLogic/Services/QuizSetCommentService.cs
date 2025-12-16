using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizSetCommentDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizSetCommentService : IQuizSetCommentService
    {
        private readonly IQuizSetCommentRepo _quizSetCommentRepo;
        private readonly IMapper _mapper;

        public QuizSetCommentService(IQuizSetCommentRepo quizSetCommentRepo, IMapper mapper)
        {
            _quizSetCommentRepo = quizSetCommentRepo;
            _mapper = mapper;
        }

        public async Task<ResponseQuizSetCommentDto> CreateAsync(RequestQuizSetCommentDto dto)
        {
            var entity = _mapper.Map<QuizSetComment>(dto);
            var created = await _quizSetCommentRepo.CreateAsync(entity);
            return _mapper.Map<ResponseQuizSetCommentDto>(created);
        }

        public async Task<ResponseQuizSetCommentDto?> GetByIdAsync(Guid id)
        {
            var entity = await _quizSetCommentRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseQuizSetCommentDto>(entity);
        }

        public async Task<PaginationResponseDto<ResponseQuizSetCommentDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizSetCommentRepo.GetAllAsync(includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizSetCommentDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseQuizSetCommentDto>> GetByUserIdAsync(Guid userId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizSetCommentRepo.GetByUserIdAsync(userId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizSetCommentDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseQuizSetCommentDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizSetCommentRepo.GetByQuizSetIdAsync(quizSetId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizSetCommentDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseQuizSetCommentDto?> UpdateAsync(Guid id, RequestQuizSetCommentDto dto)
        {
            var entity = _mapper.Map<QuizSetComment>(dto);
            var updated = await _quizSetCommentRepo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseQuizSetCommentDto>(updated);
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            return await _quizSetCommentRepo.HardDeleteAsync(id);
        }

        public async Task<int> GetCommentCountByQuizSetAsync(Guid quizSetId)
        {
            return await _quizSetCommentRepo.GetCommentCountByQuizSetAsync(quizSetId);
        }
    }
}
