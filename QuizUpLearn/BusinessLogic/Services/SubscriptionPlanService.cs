using AutoMapper;
using BusinessLogic.DTOs.SubscriptionPlanDtos;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.Extensions;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ISubscriptionPlanRepo _repo;
        private readonly IMapper _mapper;

        public SubscriptionPlanService(ISubscriptionPlanRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponseSubscriptionPlanDto>> GetAllAsync(PaginationRequestDto pagination = null!)
        {
            if(pagination == null)
            {
                pagination = new PaginationRequestDto();
            }
            var entities = await _repo.GetAllAsync();
            var dtos = _mapper.Map<List<ResponseSubscriptionPlanDto>>(entities);
            return await dtos.AsQueryable().ToPagedResponseAsync(pagination);
        }

        public async Task<ResponseSubscriptionPlanDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseSubscriptionPlanDto>(entity);
        }

        public async Task<ResponseSubscriptionPlanDto> CreateAsync(RequestSubscriptionPlanDto dto)
        {
            var entity = _mapper.Map<SubscriptionPlan>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseSubscriptionPlanDto>(created);
        }

        public async Task<ResponseSubscriptionPlanDto?> UpdateAsync(Guid id, RequestSubscriptionPlanDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);

            if (existing == null) return null;

            if(!string.IsNullOrEmpty(dto.Name))
                existing.Name = dto.Name;

            if(existing.Price != dto.Price)
                existing.Price = dto.Price;

            if(existing.DurationDays != dto.DurationDays)
                existing.DurationDays = dto.DurationDays;

            if(existing.CanAccessPremiumContent != dto.CanAccessPremiumContent)
                existing.CanAccessPremiumContent = dto.CanAccessPremiumContent;

            if(existing.CanAccessAiFeatures != dto.CanAccessAiFeatures)
                existing.CanAccessAiFeatures = dto.CanAccessAiFeatures;

            if(existing.IsActive != dto.IsActive)
                existing.IsActive = dto.IsActive;

            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _repo.UpdateAsync(id, existing);
            return updated == null ? null : _mapper.Map<ResponseSubscriptionPlanDto>(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<ResponseSubscriptionPlanDto> GetFreeSubscriptionPlanAsync()
        {
            var freePlan = await _repo.GetFreeSubscriptionPlan();
            return _mapper.Map<ResponseSubscriptionPlanDto>(freePlan);
        }
    }
}
