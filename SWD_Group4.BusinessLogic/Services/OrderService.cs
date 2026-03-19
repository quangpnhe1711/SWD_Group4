using Microsoft.EntityFrameworkCore;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.DataAccess.Context;
using SWD_Group4.DataAccess.Models;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class OrderService : IOrderService
{
    private readonly BookStoreContext _dbContext;

    public OrderService(BookStoreContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetOrderAsync(int orderId)
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
    {
        return await _dbContext.OrderItems
            .Include(oi => oi.Book)
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<Book?> GetBookByOrderItemAsync(OrderItem orderItem)
    {
        if (orderItem.Book != null)
        {
            return orderItem.Book;
        }

        if (orderItem.BookId is null)
        {
            return null;
        }

        return await _dbContext.Books.FirstOrDefaultAsync(b => b.Id == orderItem.BookId.Value);
    }

    public async Task<bool> UpdateStatusAsync(Order order, string orderStatus)
    {
        order.Status = orderStatus;
        _dbContext.Orders.Update(order);
        return await _dbContext.SaveChangesAsync() > 0;
    }
}
