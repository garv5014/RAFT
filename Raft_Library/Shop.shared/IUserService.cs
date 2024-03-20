namespace Raft_Library.Shop.shared;

public interface IUserService
{
    Task<decimal> GetUserBalanceAsync(string userId);
    Task<bool> DepositBalanceAsync(string userId, decimal amountChange);

    Task<bool> WithdrawBalanceAsync(string userId, decimal amountChange);
    Task<OrderStatus> GetOrderStatusAsync(string orderId);
    Task<OrderInfo> GetOrderInfoAsync(string orderId);
    Task<IEnumerable<OrderInfo>> GetPendingOrdersAsync(string userId);
}

public class OrderStatus
{
    public string OrderId { get; set; }
    public string Status { get; set; } // e.g., "Pending", "Completed", "Cancelled"
}

public class OrderInfo
{
    public string OrderId { get; set; }
    public Dictionary<string, int> Items { get; set; } // ItemId -> Quantity
    public decimal TotalCost { get; set; }
}
