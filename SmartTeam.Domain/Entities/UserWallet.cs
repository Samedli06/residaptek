namespace SmartTeam.Domain.Entities;

public class UserWallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public decimal Balance { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}

public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public UserWallet Wallet { get; set; } = null!;
    
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public Guid? OrderId { get; set; } // Reference to order if bonus from order
    
    public DateTime CreatedAt { get; set; }
}

public enum TransactionType
{
    Credit = 0,  // Bonus earned
    Debit = 1    // Bonus used
}
