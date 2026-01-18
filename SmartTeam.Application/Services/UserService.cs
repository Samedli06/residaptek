using AutoMapper;
using SmartTeam.Application.DTOs;
using SmartTeam.Domain.Entities;
using SmartTeam.Domain.Interfaces;

namespace SmartTeam.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    
    // Protected admin account that cannot be deleted, deactivated, or have its role changed
    private const string PROTECTED_ADMIN_EMAIL = "admin@gunaybeauty.az";

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    private bool IsProtectedAdmin(User user)
    {
        return user.Email.Equals(PROTECTED_ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Repository<User>().GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<PagedUserResultDto> SearchUsersAsync(UserSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        var query = await _unitOfWork.Repository<User>().GetAllAsync(cancellationToken);
        var users = query.AsQueryable();

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLower();
            users = users.Where(u => 
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
        }

        // Apply role filter
        if (searchDto.Role.HasValue)
        {
            users = users.Where(u => u.Role == searchDto.Role.Value);
        }

        // Apply active status filter
        if (searchDto.IsActive.HasValue)
        {
            users = users.Where(u => u.IsActive == searchDto.IsActive.Value);
        }

        // Apply date range filter
        if (searchDto.CreatedFrom.HasValue)
        {
            users = users.Where(u => u.CreatedAt >= searchDto.CreatedFrom.Value);
        }

        if (searchDto.CreatedTo.HasValue)
        {
            users = users.Where(u => u.CreatedAt <= searchDto.CreatedTo.Value);
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(searchDto.SortBy))
        {
            var isDescending = searchDto.SortOrder?.ToLower() == "desc";
            
            users = searchDto.SortBy.ToLower() switch
            {
                "firstname" => isDescending ? users.OrderByDescending(u => u.FirstName) : users.OrderBy(u => u.FirstName),
                "lastname" => isDescending ? users.OrderByDescending(u => u.LastName) : users.OrderBy(u => u.LastName),
                "email" => isDescending ? users.OrderByDescending(u => u.Email) : users.OrderBy(u => u.Email),
                "role" => isDescending ? users.OrderByDescending(u => u.Role) : users.OrderBy(u => u.Role),
                "isactive" => isDescending ? users.OrderByDescending(u => u.IsActive) : users.OrderBy(u => u.IsActive),
                "createdat" => isDescending ? users.OrderByDescending(u => u.CreatedAt) : users.OrderBy(u => u.CreatedAt),
                _ => isDescending ? users.OrderByDescending(u => u.CreatedAt) : users.OrderBy(u => u.CreatedAt)
            };
        }

        // Get total count before pagination
        var totalCount = users.Count();

        // Apply pagination
        var pageSize = Math.Max(1, Math.Min(100, searchDto.PageSize)); // Limit page size between 1 and 100
        var page = Math.Max(1, searchDto.Page);
        var skip = (page - 1) * pageSize;

        var pagedUsers = users.Skip(skip).Take(pageSize).ToList();
        var userDtos = _mapper.Map<List<UserDto>>(pagedUsers);

        // Calculate pagination info
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedUserResultDto
        {
            Users = userDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        // Prevent modifying protected admin account
        // Email cannot be changed through UpdateUserDto, so no need to check for email changes here
        if (IsProtectedAdmin(user))
        {
            // Prevent role changes
            if (updateUserDto.Role != user.Role)
            {
                throw new InvalidOperationException("Cannot change role of the protected admin account.");
            }
            // Prevent deactivation through IsActive flag
            if (updateUserDto.IsActive == false && user.IsActive == true)
            {
                throw new InvalidOperationException("Cannot deactivate the protected admin account.");
            }
        }

        _mapper.Map(updateUserDto, user);
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateUserRoleAsync(Guid userId, UpdateUserRoleDto updateUserRoleDto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        // Prevent changing role of protected admin account
        if (IsProtectedAdmin(user))
        {
            throw new InvalidOperationException("Cannot change role of the protected admin account.");
        }

        user.Role = updateUserRoleDto.Role;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        // Prevent deactivating protected admin account
        if (IsProtectedAdmin(user))
        {
            throw new InvalidOperationException("Cannot deactivate the protected admin account.");
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        // Prevent deleting protected admin account - this account cannot be deleted by anyone
        if (IsProtectedAdmin(user))
        {
            throw new InvalidOperationException("Cannot delete the protected admin account. This account is permanently protected.");
        }

        var hasActiveCarts = await _unitOfWork.Repository<Cart>()
            .AnyAsync(c => c.UserId == userId, cancellationToken);

        if (hasActiveCarts)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<User>().Update(user);
        }
        else
        {
            _unitOfWork.Repository<User>().Remove(user);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
