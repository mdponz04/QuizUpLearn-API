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

        public async Task<PaginationResponseDto<ResponseVocabularyDto>> GetAllAsync(
            PaginationRequestDto pagination,
            Repository.Enums.VocabularyDifficultyEnum? difficulty = null)
        {
            var entities = await _repo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ResponseVocabularyDto>>(entities);
            
            if (difficulty.HasValue)
            {
                dtos = dtos.Where(v => v.VocabularyDifficulty == difficulty.Value);
            }
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseVocabularyDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity != null ? _mapper.Map<ResponseVocabularyDto>(entity) : null;
        }

        public async Task<ResponseVocabularyDto?> CreateAsync(RequestVocabularyDto request)
        {
            // Validate unique KeyWord trong cùng ToeicPart
            if (await _repo.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart))
            {
                var partInfo = string.IsNullOrEmpty(request.ToeicPart) ? "no part" : $"part {request.ToeicPart}";
                throw new InvalidOperationException($"Vocabulary with keyword '{request.KeyWord}' already exists in {partInfo}.");
            }

            var entity = _mapper.Map<Vocabulary>(request);
            var created = await _repo.CreateAsync(entity);
            return created != null ? _mapper.Map<ResponseVocabularyDto>(created) : null;
        }

        public async Task<ResponseVocabularyDto?> UpdateAsync(Guid id, RequestVocabularyDto request)
        {
            // Không cho sửa từ vựng nếu đang được quiz sử dụng
            if (await _repo.HasQuizzesAsync(id))
            {
                throw new InvalidOperationException("Cannot update vocabulary that is already used by quizzes.");
            }

            // Validate unique KeyWord trong cùng ToeicPart (exclude current record)
            if (await _repo.ExistsByKeyWordAndPartAsync(request.KeyWord, request.ToeicPart, id))
            {
                var partInfo = string.IsNullOrEmpty(request.ToeicPart) ? "no part" : $"part {request.ToeicPart}";
                throw new InvalidOperationException($"Vocabulary with keyword '{request.KeyWord}' already exists in {partInfo}.");
            }

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

