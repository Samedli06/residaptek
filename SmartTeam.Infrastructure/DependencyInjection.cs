using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartTeam.Domain.Interfaces;
using SmartTeam.Infrastructure.Data;
using SmartTeam.Infrastructure.Repositories;

namespace SmartTeam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework
        services.AddDbContext<SmartTeamDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => {
                    b.MigrationsAssembly(typeof(SmartTeamDbContext).Assembly.FullName);
                    b.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    b.CommandTimeout(120);
                }
            ));

        // Add Repository Pattern
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Infrastructure Services
        services.AddScoped<SmartTeam.Application.Services.IJwtService, SmartTeam.Infrastructure.Services.JwtService>();
        services.AddScoped<SmartTeam.Application.Services.IPasswordService, SmartTeam.Infrastructure.Services.PasswordService>();
        services.AddScoped<SmartTeam.Application.Services.IFileUploadService, SmartTeam.Infrastructure.Services.FileUploadService>();
        services.AddScoped<SmartTeam.Application.Services.IEmailService, SmartTeam.Infrastructure.Services.EmailService>();
        services.AddScoped<SmartTeam.Application.Services.IImageCompressionService, SmartTeam.Infrastructure.Services.ImageCompressionService>();

        return services;
    }
}
