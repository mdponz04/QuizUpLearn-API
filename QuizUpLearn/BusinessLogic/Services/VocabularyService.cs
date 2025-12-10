using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.VocabularyDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class VocabularyService : IVocabularyService
    {
        private readonly IVocabularyRepo _repo;
        private readonly IMapper _mapper;

        public VocabularyService(IVocabularyRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponseVocabularyDto>> GetAllAsync(PaginationRequestDto pagination)
        {
            var entities = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseVocabularyDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseVocabularyDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity != null ? _mapper.Map<ResponseVocabularyDto>(entity) : null;
        }

        public async Task<ResponseVocabularyDto?> CreateAsync(RequestVocabularyDto request)
        {
            var entity = _mapper.Map<Vocabulary>(request);
            var created = await _repo.CreateAsync(entity);
            return created != null ? _mapper.Map<ResponseVocabularyDto>(created) : null;
        }

        public async Task<ResponseVocabularyDto?> UpdateAsync(Guid id, RequestVocabularyDto request)
        {
            var updated = await _repo.UpdateAsync(id, _mapper.Map<Vocabulary>(request));
            return updated != null ? _mapper.Map<ResponseVocabularyDto>(updated) : null;
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

