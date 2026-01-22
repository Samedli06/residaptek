using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public WalletService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserWalletDto> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _unitOfWork.Repository<UserWallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet == null)
        {
            wallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<UserWallet>().AddAsync(wallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new UserWalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance
        };
    }

    public async Task<UserWalletDto> GetWalletBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetOrCreateWalletAsync(userId, cancellationToken);
    }

    public async Task<WalletTransactionDto> CreditBonusAsync(Guid userId, decimal amount, string description, Guid? orderId = null, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Bonus amount must be greater than zero.");
        }

        // Get wallet directly from repo to ensure we have the entity for tracking
        var wallet = await _unitOfWork.Repository<UserWallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet == null)
        {
            // Should verify user exists first ideally, but for now allow creating wallet
             wallet = new UserWallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<UserWallet>().AddAsync(wallet, cancellationToken);
            // Save to get ID if generated (though we setting it manually)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var balanceBefore = wallet.Balance;
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Type = TransactionType.Credit,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            Description = description,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.Repository<UserWallet>().Update(wallet);
        await _unitOfWork.Repository<WalletTransaction>().AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WalletTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type,
            TypeText = transaction.Type.ToString(),
            Amount = transaction.Amount,
            BalanceBefore = transaction.BalanceBefore,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<IEnumerable<WalletTransactionDto>> GetTransactionHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _unitOfWork.Repository<UserWallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet == null)
        {
            return new List<WalletTransactionDto>();
        }

        var transactions = await _unitOfWork.Repository<WalletTransaction>()
            .FindAsync(t => t.WalletId == wallet.Id, cancellationToken);

        // Sort by date descending
        var sortedTransactions = transactions.OrderByDescending(t => t.CreatedAt).ToList();

        // Manual mapping or use AutoMapper
        return sortedTransactions.Select(t => new WalletTransactionDto
        {
            Id = t.Id,
            Type = t.Type,
            TypeText = t.Type.ToString(),
            Amount = t.Amount,
            BalanceBefore = t.BalanceBefore,
            BalanceAfter = t.BalanceAfter,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        }).ToList();
    }
}
