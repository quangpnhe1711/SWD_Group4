using SWD_Group4.DataAccess.Models;

namespace SWD_Group4.BusinessLogic.IServices;

public interface IOrderService
{
    Task<Order?> GetOrderAsync(int orderId);

    Task<List<OrderItem>> GetOrderItemsAsync(int orderId);

    Task<Book?> GetBookByOrderItemAsync(OrderItem orderItem);

    Task<bool> UpdateStatusAsync(Order order, string orderStatus);
}
