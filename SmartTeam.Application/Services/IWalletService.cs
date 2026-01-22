using SmartTeam.Application.DTOs;

namespace SmartTeam.Application.Services;

public interface IWalletService
{
    Task<UserWalletDto> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserWalletDto> GetWalletBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WalletTransactionDto> CreditBonusAsync(Guid userId, decimal amount, string description, Guid? orderId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletTransactionDto>> GetTransactionHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
}
