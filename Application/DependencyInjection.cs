using Application.Interfaces;
using Application.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application layer services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Configuration for JWT and other settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register AutoMapper profiles from this assembly
        services.AddAutoMapper(configAction => 
        {
            configAction.AddMaps(Assembly.GetExecutingAssembly());
        });

		// Register FluentValidation validators from this assembly
		services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Register services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IUserService, UserService>();
        
        // Register book and category management services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookCopyService, BookCopyService>();
        
        return services;
    }
}