using SWD_Group4.BusinessLogic.IServices;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class PaymentService : IPaymentService
{
    public bool Refund(string transactionId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || amount <= 0)
        {
            return false;
        }

        // Single payment gateway simulation: any transaction id ending with "FAIL" fails.
        return !transactionId.EndsWith("FAIL", StringComparison.OrdinalIgnoreCase);
    }
}
