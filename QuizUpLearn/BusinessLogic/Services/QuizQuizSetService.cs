using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.QuizQuizSetDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class QuizQuizSetService : IQuizQuizSetService
    {
        private readonly IQuizQuizSetRepo _quizQuizSetRepo;
        private readonly IQuizRepo _quizRepo;
        private readonly IQuizSetRepo _quizSetRepo;
        private readonly IMapper _mapper;

        public QuizQuizSetService(
            IQuizQuizSetRepo quizQuizSetRepo,
            IQuizRepo quizRepo,
            IQuizSetRepo quizSetRepo,
            IMapper mapper)
        {
            _quizQuizSetRepo = quizQuizSetRepo;
            _quizRepo = quizRepo;
            _quizSetRepo = quizSetRepo;
            _mapper = mapper;
        }

        public async Task<ResponseQuizQuizSetDto> CreateAsync(RequestQuizQuizSetDto dto)
        {
            if(dto.QuizId == null || dto.QuizSetId == null)
                throw new ArgumentException("QuizId and QuizSetId cannot be null");
            
            var quiz = await _quizRepo.GetQuizByIdAsync(dto.QuizId.Value);
            if (quiz == null)
                throw new ArgumentException($"Quiz with ID {dto.QuizId} not found");

            var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(dto.QuizSetId.Value);
            if (quizSet == null)
                throw new ArgumentException($"Quiz set with ID {dto.QuizSetId} not found");

            var exists = await _quizQuizSetRepo.IsExistedAsync(dto.QuizId.Value, dto.QuizSetId.Value);
            if (exists)
                throw new InvalidOperationException($"Quiz {dto.QuizId} is already associated with Quiz Set {dto.QuizSetId}");

            var entity = _mapper.Map<QuizQuizSet>(dto);
            var created = await _quizQuizSetRepo.CreateAsync(entity);
            return _mapper.Map<ResponseQuizQuizSetDto>(created);
        }

        public async Task<ResponseQuizQuizSetDto?> GetByIdAsync(Guid id)
        {
            var entity = await _quizQuizSetRepo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseQuizQuizSetDto>(entity);
        }

        public async Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetAllAsync(PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizQuizSetRepo.GetAllAsync(includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizQuizSetDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetByQuizIdAsync(Guid quizId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizQuizSetRepo.GetByQuizIdAsync(quizId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizQuizSetDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<PaginationResponseDto<ResponseQuizQuizSetDto>> GetByQuizSetIdAsync(Guid quizSetId, PaginationRequestDto pagination, bool includeDeleted = false)
        {
            var entities = await _quizQuizSetRepo.GetByQuizSetIdAsync(quizSetId, includeDeleted);
            var dtos = _mapper.Map<IEnumerable<ResponseQuizQuizSetDto>>(entities);
            return dtos.ToPagedResponse(pagination);
        }

        public async Task<ResponseQuizQuizSetDto?> GetByQuizAndQuizSetAsync(Guid quizId, Guid quizSetId, bool includeDeleted = false)
        {
            var entity = await _quizQuizSetRepo.GetByQuizAndQuizSetAsync(quizId, quizSetId, includeDeleted);
            return entity == null ? null : _mapper.Map<ResponseQuizQuizSetDto>(entity);
        }

        public async Task<ResponseQuizQuizSetDto?> UpdateAsync(Guid id, RequestQuizQuizSetDto dto)
        {
            if(dto.QuizId == null || dto.QuizSetId == null)
                throw new ArgumentException("QuizId and QuizSetId cannot be null");
            // Validate that quiz and quiz set exist
            var quiz = await _quizRepo.GetQuizByIdAsync(dto.QuizId.Value);
            if (quiz == null)
                throw new ArgumentException($"Quiz with ID {dto.QuizId} not found");

            var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(dto.QuizSetId.Value);
            if (quizSet == null)
                throw new ArgumentException($"Quiz set with ID {dto.QuizSetId} not found");

            var entity = _mapper.Map<QuizQuizSet>(dto);
            var updated = await _quizQuizSetRepo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseQuizQuizSetDto>(updated);
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            return await _quizQuizSetRepo.SoftDeleteAsync(id);
        }

        public async Task<bool> HardDeleteAsync(Guid id)
        {
            return await _quizQuizSetRepo.HardDeleteAsync(id);
        }

        public async Task<bool> IsExistedAsync(Guid quizId, Guid quizSetId)
        {
            return await _quizQuizSetRepo.IsExistedAsync(quizId, quizSetId);
        }

        public async Task<int> GetQuizCountByQuizSetAsync(Guid quizSetId)
        {
            return await _quizQuizSetRepo.GetQuizCountByQuizSetAsync(quizSetId);
        }

        public async Task<bool> AddQuizToQuizSetAsync(Guid quizId, Guid quizSetId)
        {
            try
            {
                var dto = new RequestQuizQuizSetDto { QuizId = quizId, QuizSetId = quizSetId };
                await CreateAsync(dto);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveQuizFromQuizSetAsync(Guid quizId, Guid quizSetId)
        {
            var entity = await _quizQuizSetRepo.GetByQuizAndQuizSetAsync(quizId, quizSetId);
            if (entity == null) return false;

            return await _quizQuizSetRepo.SoftDeleteAsync(entity.Id);
        }

        public async Task<bool> AddQuizzesToQuizSetAsync(List<Guid> quizIds, Guid quizSetId)
        {
            try
            {
                var quizSet = await _quizSetRepo.GetQuizSetByIdAsync(quizSetId);
                if (quizSet == null)
                    throw new ArgumentException($"Quiz set with ID {quizSetId} not found");

                var dtos = new List<RequestQuizQuizSetDto>();
                foreach (var quizId in quizIds)
                {
                    var quiz = await _quizRepo.GetQuizByIdAsync(quizId);
                    if (quiz == null) continue;

                    var exists = await _quizQuizSetRepo.IsExistedAsync(quizId, quizSetId);
                    if (exists) continue;

                    dtos.Add(new RequestQuizQuizSetDto
                    {
                        QuizId = quizId,
                        QuizSetId = quizSetId
                    });
                }

                if (dtos.Any())
                {
                    var entities = _mapper.Map<IEnumerable<QuizQuizSet>>(dtos);
                    await _quizQuizSetRepo.AddRangeAsync(entities);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed while add quizzes to quiz set: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteByQuizIdAsync(Guid quizId)
        {
            return await _quizQuizSetRepo.DeleteByQuizIdAsync(quizId);
        }

        public async Task<bool> DeleteByQuizSetIdAsync(Guid quizSetId)
        {
            return await _quizQuizSetRepo.DeleteByQuizSetIdAsync(quizSetId);
        }
    }
}