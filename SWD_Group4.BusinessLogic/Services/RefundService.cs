using Microsoft.EntityFrameworkCore;
using SWD_Group4.BusinessLogic.DTO;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.DataAccess.Context;
using SWD_Group4.DataAccess.Models;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class RefundService : IRefundService
{
    private const string RefundStatusPending = "Pending";
    private const string RefundStatusApproved = "Approved";
    private const string RefundStatusProcessed = "Processed";
    private const string RefundStatusRejected = "Rejected";
    private const string RefundStatusFailed = "Failed";
    private const string OrderStatusRefunded = "Refunded";

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RefundStatusPending] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                RefundStatusApproved,
                RefundStatusRejected
            },
            [RefundStatusApproved] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                RefundStatusProcessed,
                RefundStatusFailed
            },
            [RefundStatusRejected] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [RefundStatusProcessed] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [RefundStatusFailed] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

    private readonly BookStoreContext _dbContext;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public RefundService(
        BookStoreContext dbContext,
        IOrderService orderService,
        IPaymentService paymentService)
    {
        _dbContext = dbContext;
        _orderService = orderService;
        _paymentService = paymentService;
    }

    public async Task<List<RefundDto>> GetRefundRequestsAsync(int sellerId)
    {
        var refundRequests = await _dbContext.RefundRequests
            .Include(rr => rr.Order)
                .ThenInclude(o => o!.OrderItems)
                    .ThenInclude(oi => oi.Book)
            .Where(rr => rr.Order != null && rr.Order.SellerId == sellerId)
            .ToListAsync();

        var result = new List<RefundDto>();

        foreach (var refundRequest in refundRequests)
        {
            if (refundRequest.Order == null)
            {
                continue;
            }

            var orderItems = await _orderService.GetOrderItemsAsync(refundRequest.Order.Id);
            var itemDtos = new List<OrderItemDto>();

            foreach (var orderItem in orderItems)
            {
                var book = await _orderService.GetBookByOrderItemAsync(orderItem);
                itemDtos.Add(new OrderItemDto
                {
                    OrderItemId = orderItem.Id,
                    Quantity = orderItem.Quantity ?? 0,
                    Price = orderItem.Price ?? 0,
                    Book = book == null
                        ? null
                        : new BookDto
                        {
                            BookId = book.Id,
                            Title = book.Title ?? string.Empty,
                            Author = book.Author ?? string.Empty,
                            Price = book.Price ?? 0
                        }
                });
            }

            result.Add(new RefundDto
            {
                RefundId = refundRequest.Id,
                CustomerId = refundRequest.Order.CustomerId ?? 0,
                Reason = refundRequest.Reason ?? string.Empty,
                RefundRequestStatus = refundRequest.Status ?? string.Empty,
                Order = new OrderDto
                {
                    OrderId = refundRequest.Order.Id,
                    CustomerId = refundRequest.Order.CustomerId ?? 0,
                    OrderItems = itemDtos,
                    TotalPrice = refundRequest.Order.TotalPrice ?? 0,
                    OrderStatus = refundRequest.Order.Status ?? string.Empty
                }
            });
        }

        return result;
    }

    public async Task<bool> ApproveRefundAsync(int refundId)
    {
        var refundRequest = await _dbContext.RefundRequests
            .Include(rr => rr.Order)
            .FirstOrDefaultAsync(rr => rr.Id == refundId);

        if (refundRequest == null || refundRequest.Order == null)
        {
            return false;
        }

        var approved = await UpdateStatusAsync(refundRequest, RefundStatusApproved);
        if (!approved)
        {
            return false;
        }

        return await ProcessRefundAsync(refundRequest.Order, refundRequest);
    }

    public async Task<bool> RejectRefundAsync(int refundId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        var refundRequest = await _dbContext.RefundRequests.FirstOrDefaultAsync(rr => rr.Id == refundId);
        if (refundRequest == null)
        {
            return false;
        }

        refundRequest.Reason = reason.Trim();
        return await UpdateStatusAsync(refundRequest, RefundStatusRejected);
    }

    public async Task<bool> ProcessRefundAsync(Order order, RefundRequest refundRequest)
    {
        if (string.IsNullOrWhiteSpace(order.PaymentTransactionId))
        {
            await UpdateStatusAsync(refundRequest, RefundStatusFailed);
            return false;
        }

        var amount = Convert.ToDecimal(order.TotalPrice ?? 0d);
        var paymentResult = _paymentService.Refund(order.PaymentTransactionId, amount);

        if (!paymentResult)
        {
            await UpdateStatusAsync(refundRequest, RefundStatusFailed);
            return false;
        }

        var updated = await UpdateStatusAsync(refundRequest, RefundStatusProcessed);
        if (!updated)
        {
            return false;
        }

        return await _orderService.UpdateStatusAsync(order, OrderStatusRefunded);
    }

    public async Task<bool> UpdateStatusAsync(RefundRequest refundRequest, string refundRequestStatus)
    {
        if (!CanTransition(refundRequest.Status, refundRequestStatus))
        {
            return false;
        }

        refundRequest.Status = refundRequestStatus;
        _dbContext.RefundRequests.Update(refundRequest);
        return await _dbContext.SaveChangesAsync() > 0;
    }

    private static bool CanTransition(string? currentStatus, string targetStatus)
    {
        if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(targetStatus))
        {
            return false;
        }

        if (!AllowedTransitions.TryGetValue(currentStatus, out var nextStatuses))
        {
            return false;
        }

        return nextStatuses.Contains(targetStatus);
    }
}
