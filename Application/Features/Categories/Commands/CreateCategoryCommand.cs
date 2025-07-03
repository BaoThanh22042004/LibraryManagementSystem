using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Categories.Commands;

/// <summary>
/// Command to create a new category (UC037 - Create Category).
/// </summary>
/// <remarks>
/// Implements UC037 specifications with the following requirements:
/// - PRE-1: Staff member must be authenticated with category management permissions (enforced at controller level)
/// - PRE-2: Category name must be provided and meet length requirements (1-100 characters)
/// - PRE-3: Optional description must not exceed 500 characters if provided
/// - PRE-4: Category name must be unique in the system (case-insensitive)
/// 
/// Ensures the following postconditions:
/// - POST-1: New category is created and available for book assignments
/// - POST-2: Category appears in all category lists and selection interfaces
/// - POST-3: Staff can immediately assign books to the new category
/// - POST-4: Category cover image is stored if provided
/// - POST-5: Success confirmation is displayed to staff member (at controller level)
/// </remarks>
public record CreateCategoryCommand(CreateCategoryDto CategoryDto) : IRequest<Result<int>>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var categoryRepository = _unitOfWork.Repository<Category>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();

            // PRE-2: Validate category name length (1-100 characters)
            if (string.IsNullOrWhiteSpace(request.CategoryDto.Name))
                return Result.Failure<int>("Category name is required."); // UC037.E2: Invalid Category Information
                
            if (request.CategoryDto.Name.Length < 1 || request.CategoryDto.Name.Length > 100)
                return Result.Failure<int>("Category name must be between 1 and 100 characters."); // UC037.E2: Invalid Category Information
                
            // PRE-3: Validate optional description length (max 500 characters)
            if (request.CategoryDto.Description != null && request.CategoryDto.Description.Length > 500)
                return Result.Failure<int>("Category description must not exceed 500 characters."); // UC037.E2: Invalid Category Information

            // PRE-4: Check if category name already exists (case-insensitive)
            var nameExists = await categoryRepository.ExistsAsync(c => c.Name.ToLower() == request.CategoryDto.Name.ToLower());
            if (nameExists)
                return Result.Failure<int>($"Category with name '{request.CategoryDto.Name}' already exists."); // UC037.E1: Category Name Already Exists

            // Create new category entity
            var category = _mapper.Map<Category>(request.CategoryDto);
            category.CreatedAt = DateTime.UtcNow;
            
            await categoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Create detailed audit log entry for category creation (BR-22)
            var auditLog = new AuditLog
            {
                EntityType = "Category",
                EntityId = category.Id.ToString(),
                EntityName = category.Name,
                ActionType = AuditActionType.Create,
                Details = $"Created new category: {category.Name}",
                Module = "Category Management",
                AfterState = $"Name: {category.Name}, Description: {category.Description?.Substring(0, Math.Min(category.Description.Length, 100))}{(category.Description?.Length > 100 ? "..." : "")}, HasCoverImage: {!string.IsNullOrEmpty(category.CoverImageUrl)}",
                IsSuccess = true
            };
            
            await auditRepository.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(category.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            
            // Log the exception
            try
            {
                var auditRepository = _unitOfWork.Repository<AuditLog>();
                await auditRepository.AddAsync(new AuditLog
                {
                    EntityType = "Category",
                    ActionType = AuditActionType.Create,
                    Details = $"Failed to create category: {request.CategoryDto.Name}",
                    ErrorMessage = ex.Message,
                    Module = "Category Management",
                    IsSuccess = false
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Ignore exceptions in error logging
            }
            
            return Result.Failure<int>($"Failed to create category: {ex.Message}"); // UC037.E4: System Error
        }
    }
}