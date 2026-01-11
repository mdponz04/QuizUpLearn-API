using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class AnswerOptionService : IAnswerOptionService
    {
        private readonly IAnswerOptionRepo _repo;
        private readonly IMapper _mapper;

        public AnswerOptionService(IAnswerOptionRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<ResponseAnswerOptionDto> CreateAnswerOptionAsync(RequestAnswerOptionDto dto)
        {
            var entity = _mapper.Map<AnswerOption>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseAnswerOptionDto>(created);
        }

        public async Task<IEnumerable<ResponseAnswerOptionDto>> GetAllAnswerOptionAsync(bool includeDeleted = false)
        {
            var list = await _repo.GetAllAsync(includeDeleted);
            return _mapper.Map<IEnumerable<ResponseAnswerOptionDto>>(list);
        }

        public async Task<ResponseAnswerOptionDto?> GetAnswerOptionByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseAnswerOptionDto>(entity);
        }

        public async Task<IEnumerable<ResponseAnswerOptionDto>> GetAnswerOptionByQuizIdAsync(Guid quizId, bool includeDeleted = false)
        {
            var list = await _repo.GetByQuizIdAsync(quizId, includeDeleted);
            return _mapper.Map<IEnumerable<ResponseAnswerOptionDto>>(list);
        }

        public async Task<bool> RestoreAnswerOptionAsync(Guid id)
        {
            return await _repo.RestoreAsync(id);
        }

        public async Task<bool> DeleteAnswerOptionAsync(Guid id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<ResponseAnswerOptionDto?> UpdateAnswerOptionAsync(Guid id, RequestAnswerOptionDto dto)
        {
            var entity = _mapper.Map<AnswerOption>(dto);
            var updated = await _repo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseAnswerOptionDto>(updated);
        }
    }
}
