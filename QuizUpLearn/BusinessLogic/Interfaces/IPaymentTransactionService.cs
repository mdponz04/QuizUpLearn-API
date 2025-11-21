using BusinessLogic.DTOs;
using BusinessLogic.DTOs.PaymentTransactionDtos;
using BusinessLogic.DTOs.PaymentTransactionDtos;
using Repository.Entities;

namespace BusinessLogic.Interfaces
{
    public interface IPaymentTransactionService
    {
        Task<PaginationResponseDto<ResponsePaymentTransactionDto>> GetAllAsync(PaginationRequestDto pagination);
        Task<ResponsePaymentTransactionDto?> GetByIdAsync(Guid id);
        Task<ResponsePaymentTransactionDto> CreateAsync(RequestPaymentTransactionDto dto);
        Task<ResponsePaymentTransactionDto?> UpdateAsync(Guid id, RequestPaymentTransactionDto dto);
        Task<PaymentTransaction?> GetByPaymentGatewayTransactionOrderCodeAsync(string orderCode);
        Task<bool> DeleteAsync(Guid id);
    }
}
