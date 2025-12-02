using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.DTOs.SubscriptionDtos;
using BusinessLogic.Extensions;
using BusinessLogic.Interfaces;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepo _repo;
        private readonly IMapper _mapper;
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public SubscriptionService(ISubscriptionRepo repo, IMapper mapper, ISubscriptionPlanService subscriptionPlanService)
        {
            _repo = repo;
            _mapper = mapper;
            _subscriptionPlanService = subscriptionPlanService;
        }

        public async Task<PaginationResponseDto<ResponseSubscriptionDto>> GetAllAsync(PaginationRequestDto pagination = null!)
        {
            if(pagination == null)
            {
                pagination = new PaginationRequestDto();
            }
            var entities = await _repo.GetAllAsync();
            var dtos = _mapper.Map<List<ResponseSubscriptionDto>>(entities);
            return await dtos.AsQueryable().ToPagedResponseAsync(pagination);
        }

        public async Task<ResponseSubscriptionDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseSubscriptionDto>(entity);
        }

        public async Task<ResponseSubscriptionDto> CreateAsync(RequestSubscriptionDto dto)
        {
            var entity = _mapper.Map<Subscription>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponseSubscriptionDto>(created);
        }

        public async Task<ResponseSubscriptionDto?> UpdateAsync(Guid id, RequestSubscriptionDto dto)
        {
            var entity = _mapper.Map<Subscription>(dto);
            var updated = await _repo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponseSubscriptionDto>(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<ResponseSubscriptionDto?> GetByUserIdAsync(Guid userId)
        {
            var entity = await _repo.GetByUserIdAsync(userId);
            if (entity == null) 
                return null;

            var subscription = _mapper.Map<ResponseSubscriptionDto>(entity);

            // Check if subscription is expired and not already free
            if (subscription.EndDate.HasValue && 
                subscription.EndDate.Value < DateTime.UtcNow)
            {
                var freePlan = await _subscriptionPlanService.GetFreeSubscriptionPlanAsync();
                
                if (freePlan != null && subscription.SubscriptionPlanId != freePlan.Id)
                {
                    var updatedSubscription = await UpdateAsync(subscription.Id, new RequestSubscriptionDto
                    {
                        UserId = subscription.UserId,
                        SubscriptionPlanId = freePlan.Id,
                        AiGenerateQuizSetRemaining = subscription.AiGenerateQuizSetRemaining,//Retain remaining usage
                        EndDate = DateTime.UtcNow.AddDays(freePlan.DurationDays)
                    });

                    return updatedSubscription;
                }
            }

            return subscription;
        }

        public async Task<ResponseSubscriptionDto?> CalculateRemainingUsageByUserId(Guid userId, int usedQuantity)
        {
            var entity = await _repo.CalculateRemainingUsageByUserId(userId, usedQuantity);
            return entity == null ? null : _mapper.Map<ResponseSubscriptionDto>(entity);
        }

        public async Task<IEnumerable<ResponseSubscriptionDto>> GetByPlanIdAsync(Guid subscriptionPlanId)
        {
            return _mapper.Map<IEnumerable<ResponseSubscriptionDto>>(await _repo.GetByPlanIdAsync(subscriptionPlanId));
        }
    }
}
