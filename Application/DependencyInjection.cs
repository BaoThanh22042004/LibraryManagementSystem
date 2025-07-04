using Application.Behaviors;
using Application.Common.Security;
using Application.Mappings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Application.Features.Users.Commands;
using Application.Interfaces;
using AutoMapper;
using Application.Services;
using Application.Interfaces.Services;

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
        var assembly = Assembly.GetExecutingAssembly();
        
        // Register AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));
        
        // Register MediatR
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });
        
        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly);
        
        // Register security services
        services.AddSingleton<ITokenGenerator>(provider => {
            var jwtSettings = configuration.GetSection("JwtSettings");
            return new JwtTokenGenerator(
                jwtSettings["SecretKey"] ?? "YourSuperSecretKey1234567890!@#$%^&*()",
                jwtSettings["Issuer"] ?? "LibraryManagementSystem",
                jwtSettings["Audience"] ?? "LibraryUsers",
                int.Parse(jwtSettings["ExpiryMinutes"] ?? "60")
            );
        });
        
        // Register Services
        services.AddScoped<IEmailService, EmailService>();
        
        return services;
    }
}