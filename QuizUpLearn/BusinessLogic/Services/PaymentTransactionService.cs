using AutoMapper;
using BusinessLogic.DTOs.PaymentTransactionDtos;
using BusinessLogic.DTOs.TransactionDtos;
using BusinessLogic.DTOs;
using BusinessLogic.Interfaces;
using BusinessLogic.Extensions;
using Repository.Entities;
using Repository.Interfaces;

namespace BusinessLogic.Services
{
    public class PaymentTransactionService : IPaymentTransactionService
    {
        private readonly IPaymentTransactionRepo _repo;
        private readonly IMapper _mapper;

        public PaymentTransactionService(IPaymentTransactionRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PaginationResponseDto<ResponsePaymentTransactionDto>> GetAllAsync(PaginationRequestDto pagination = null!)
        {
            if(pagination == null)
            {
                pagination = new PaginationRequestDto();
            }

            var entities = await _repo.GetAllAsync();
            var dtos = _mapper.Map<List<ResponsePaymentTransactionDto>>(entities);
            return await dtos.AsQueryable().ToPagedResponseAsync(pagination);
        }

        public async Task<ResponsePaymentTransactionDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponsePaymentTransactionDto>(entity);
        }

        public async Task<ResponsePaymentTransactionDto> CreateAsync(RequestPaymentTransactionDto dto)
        {
            var entity = _mapper.Map<PaymentTransaction>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<ResponsePaymentTransactionDto>(created);
        }

        public async Task<ResponsePaymentTransactionDto?> UpdateAsync(Guid id, RequestPaymentTransactionDto dto)
        {
            var entity = _mapper.Map<PaymentTransaction>(dto);
            var updated = await _repo.UpdateAsync(id, entity);
            return updated == null ? null : _mapper.Map<ResponsePaymentTransactionDto>(updated);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _repo.DeleteAsync(id);
        }
    }
}
