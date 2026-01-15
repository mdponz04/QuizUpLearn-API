using BusinessLogic.DTOs.PaymentTransactionDtos;
using BusinessLogic.DTOs.SubscriptionDtos;
using BusinessLogic.Interfaces;
using Net.payOS.Types;

namespace BusinessLogic.Services
{
    public class BuySubscriptionService : IBuySubscriptionService
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IPaymentService _paymentService;
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public BuySubscriptionService(IPaymentTransactionService paymentTransactionService, IPaymentService paymentService, ISubscriptionPlanService subscriptionPlanService, ISubscriptionService subscriptionService)
        {
            _paymentTransactionService = paymentTransactionService;
            _paymentService = paymentService;
            _subscriptionPlanService = subscriptionPlanService;
            _subscriptionService = subscriptionService;
        }

        public async Task HandlePaymentCancelAsync(long orderCode)
        {
            if(orderCode <= 0)
            {
                throw new ArgumentException("Invalid order code");
            }
            var transaction = await _paymentTransactionService.GetByPaymentGatewayTransactionOrderCodeAsync(orderCode.ToString());

            if(transaction == null)
            {
                throw new InvalidOperationException("Transaction not found");
            }

            await _paymentTransactionService.UpdateAsync(transaction!.Id, new RequestPaymentTransactionDto
            {
                UserId = transaction.UserId,
                SubscriptionPlanId = transaction.SubscriptionPlanId,
                Amount = transaction.Amount,
                PaymentGatewayTransactionId = transaction.PaymentGatewayTransactionId,
                Status = Repository.Enums.TransactionStatusEnum.Failed,
                CompletedDate = DateTime.UtcNow
            });
        }

        public async Task HandlePaymentSuccessAsync(long orderCode)
        {
            var transaction = await _paymentTransactionService.GetByPaymentGatewayTransactionOrderCodeAsync(orderCode.ToString());

            if (transaction == null || transaction.Status == Repository.Enums.TransactionStatusEnum.Completed)
            {
                return;
            }

            await _paymentTransactionService.UpdateAsync(transaction!.Id, new RequestPaymentTransactionDto
            {
                UserId = transaction.UserId,
                SubscriptionPlanId = transaction.SubscriptionPlanId,
                Amount = transaction.Amount,
                PaymentGatewayTransactionId = transaction.PaymentGatewayTransactionId,
                Status = Repository.Enums.TransactionStatusEnum.Completed,
                CompletedDate = DateTime.UtcNow
            });

            var freePlan = await _subscriptionPlanService.GetFreeSubscriptionPlanAsync();
            var plan = await _subscriptionPlanService.GetByIdAsync(transaction.SubscriptionPlanId);
            
            if (plan == null)
                throw new Exception("Subscription plan not found");
            
            var subscription = await _subscriptionService.GetByUserIdAsync(transaction.UserId);
            
            DateTime? startDate = DateTime.UtcNow;

            if (subscription == null)
            {
                subscription = await _subscriptionService.CreateAsync(new RequestSubscriptionDto
                {
                    UserId = transaction.UserId,
                    SubscriptionPlanId = plan.Id,
                    EndDate = startDate.Value.AddDays(plan.DurationDays)
                });
            }
            else
            {
                if (subscription.EndDate != null 
                    && subscription.EndDate > DateTime.UtcNow
                    && subscription.SubscriptionPlanId != freePlan.Id)
                {
                    startDate = subscription.EndDate;
                }

                await _subscriptionService.UpdateAsync(subscription.Id, new RequestSubscriptionDto
                {
                    SubscriptionPlanId = transaction.SubscriptionPlanId,
                    EndDate = startDate.Value.AddDays(plan.DurationDays)
                });
            }
        }

        public async Task<(long, string)> StartSubscriptionPurchaseAsync(Guid userId, Guid planId, string successUrl, string canceledUrl)
        {
            var plan = await _subscriptionPlanService.GetByIdAsync(planId);

            if (plan == null)
                return (-1, "");

            var items = new List<ItemData>
            {
                new ItemData(plan.Name, 1, (int)plan.Price)
            };

            var paymentInfo = await _paymentService.CreatePaymentLinkAsync((int) plan.Price, $"QUL sub {plan.Name}", items, successUrl, canceledUrl);

            if (paymentInfo == null)
                return (-1, "");

            var transaction = await _paymentTransactionService.CreateAsync(new RequestPaymentTransactionDto
            {
                UserId = userId,
                SubscriptionPlanId = planId,
                Amount = plan.Price,
                PaymentGatewayTransactionId = paymentInfo!.orderCode.ToString()
            });

            return (paymentInfo!.orderCode, paymentInfo!.checkoutUrl);
        }
    }
}
