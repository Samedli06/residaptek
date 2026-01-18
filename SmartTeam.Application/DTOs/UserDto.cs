using SmartTeam.Domain.Entities;

namespace SmartTeam.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public string RoleName => Role.ToString();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.NormalUser;
}

public class UpdateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateUserRoleDto
{
    public UserRole Role { get; set; }
}

public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int AdminUsers { get; set; }
    public int NormalUsers { get; set; }
}

public class UserSearchDto
{
    public string? SearchTerm { get; set; } // Search in name, email, phone
    public UserRole? Role { get; set; } // Filter by role
    public bool? IsActive { get; set; } // Filter by active status
    public DateTime? CreatedFrom { get; set; } // Filter by creation date range
    public DateTime? CreatedTo { get; set; }
    public string? SortBy { get; set; } = "CreatedAt"; // Sort field
    public string? SortOrder { get; set; } = "desc"; // asc or desc
    public int Page { get; set; } = 1; // Pagination
    public int PageSize { get; set; } = 10; // Items per page
}

public class PagedUserResultDto
{
    public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
