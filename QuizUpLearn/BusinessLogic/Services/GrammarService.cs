using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.GrammarDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class GrammarService : IGrammarService
    {
        private readonly IGrammarRepo _repo;
        private readonly IMapper _mapper;

        public GrammarService(IGrammarRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponseGrammarDto>> GetAllAsync(PaginationRequestDto pagination)
        {
            var entities = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseGrammarDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseGrammarDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity != null ? _mapper.Map<ResponseGrammarDto>(entity) : null;
        }

        public async Task<ResponseGrammarDto?> CreateAsync(RequestGrammarDto request)
        {
            // Validate unique Name
            if (await _repo.ExistsByNameAsync(request.Name))
            {
                throw new InvalidOperationException($"Grammar with name '{request.Name}' already exists.");
            }

            var entity = _mapper.Map<Grammar>(request);
            var created = await _repo.CreateAsync(entity);
            return created != null ? _mapper.Map<ResponseGrammarDto>(created) : null;
        }

        public async Task<ResponseGrammarDto?> UpdateAsync(Guid id, RequestGrammarDto request)
        {
            // Validate unique Name (exclude current record)
            if (await _repo.ExistsByNameAsync(request.Name, id))
            {
                throw new InvalidOperationException($"Grammar with name '{request.Name}' already exists.");
            }

            var updated = await _repo.UpdateAsync(id, _mapper.Map<Grammar>(request));
            return updated != null ? _mapper.Map<ResponseGrammarDto>(updated) : null;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            // Không cho xoá khi vẫn còn quiz phụ thuộc
            if (await _repo.HasQuizzesAsync(id))
            {
                return false;
            }

            return await _repo.DeleteAsync(id);
        }
    }
}

