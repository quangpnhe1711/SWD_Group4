namespace SWD_Group4.BusinessLogic.IServices;

public interface IPaymentService
{
    bool Refund(string transactionId, decimal amount);
}
