using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository.DBContext;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class PaymentTransactionRepo : IPaymentTransactionRepo
    {
        private readonly MyDbContext _context;

        public PaymentTransactionRepo(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PaymentTransaction>> GetAllAsync()
        {
            return await _context.PaymentTransactions.ToListAsync();
        }

        public async Task<PaymentTransaction?> GetByIdAsync(Guid id)
        {
            return await _context.PaymentTransactions.FindAsync(id);
        }

        public async Task<PaymentTransaction> CreateAsync(PaymentTransaction paymentTransaction)
        {
            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();
            return paymentTransaction;
        }

        public async Task<PaymentTransaction?> UpdateAsync(Guid id, PaymentTransaction paymentTransaction)
        {
            var existing = await _context.PaymentTransactions.FindAsync(id);
            if (existing == null) return null;

            if(paymentTransaction.Amount != 0)
                existing.Amount = paymentTransaction.Amount;
            if(paymentTransaction.PaymentGatewayTransactionId != null)
                existing.PaymentGatewayTransactionId = paymentTransaction.PaymentGatewayTransactionId;

            _context.PaymentTransactions.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.PaymentTransactions.FindAsync(id);
            if (entity == null) return false;

            _context.PaymentTransactions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
