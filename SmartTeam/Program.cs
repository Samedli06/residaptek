
using SmartTeam.Infrastructure;
using SmartTeam.Application;
using SmartTeam.Application.Configuration;
using SmartTeam.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace SmartTeam
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplication();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:5173", 
                        "https://localhost:5173",
                        "http://localhost:7222", 
                        "https://localhost:7222",
                        "https://e-depo.netlify.app",
                        "https://e-depo.az",
                        "https://www.e-depo.az"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains();
                });
            });


            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.Configure<SmartTeam.Infrastructure.Services.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.Configure<SmartTeam.Application.Configuration.BonusSettings>(builder.Configuration.GetSection("BonusSettings"));
            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings!.SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Residaptek E-Commerce API",
                    Version = "v1",
                    Description = "A comprehensive beauty e-commerce API with role-based pricing and JWT authentication"
                });

                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

                options.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });

                options.MapType<IFormFileCollection>(() => new Microsoft.OpenApi.Models.OpenApiSchema
                {
                    Type = "array",
                    Items = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                });

                options.EnableAnnotations();
                options.OperationFilter<FileUploadOperationFilter>();
            });

            var app = builder.Build();

            // Add request logging for debugging
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var origin = context.Request.Headers.Origin.FirstOrDefault() ?? "Unknown";
                logger.LogInformation("Request: {Method} {Path} from {Origin}", 
                    context.Request.Method, context.Request.Path, origin);
                await next();
            });

            app.UseCors("AllowFrontend");


            // Initialize database with retry logic
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SmartTeam.Infrastructure.Data.SmartTeamDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    // Apply pending migrations
                    await context.Database.MigrateAsync();
                    
                    // Always run initializer to ensure admin user exists
                    await SmartTeam.Infrastructure.Data.DbInitializer.InitializeAsync(context);
                    logger.LogInformation("Database initialization completed");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while initializing the database. The application will continue but may not function properly.");
                }
            }

            // Enable Swagger only in Development
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Residaptek API v1");
                    options.RoutePrefix = "swagger";
                });
            }

            // Disable HTTPS redirection to avoid mixed content issues
            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseDefaultFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            // Use Developer Exception Page only in Development
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePages();
            }

            app.MapControllers();

            app.MapFallbackToFile("index.html");
            app.Run();
        }
    }
}
