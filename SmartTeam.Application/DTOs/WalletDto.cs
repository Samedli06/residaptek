using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class UserWalletDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}

public class WalletTransactionDto
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; }
    public string TypeText { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
