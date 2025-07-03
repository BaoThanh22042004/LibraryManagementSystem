using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Categories.Commands;

/// <summary>
/// Command to update an existing category (UC038 - Update Category).
/// </summary>
/// <remarks>
/// Implements UC038 specifications with the following requirements:
/// - PRE-1: Category must exist in the system
/// - PRE-2: Staff member must have category management permissions (enforced at controller level)
/// - PRE-3: Updated category name must be unique if changed (excluding current category)
/// - PRE-4: Updated description must meet length requirements if provided
/// - PRE-5: System must support cover image management
/// 
/// Ensures the following postconditions:
/// - POST-1: Category information is updated with new values
/// - POST-2: All books assigned to category continue to show updated information
/// - POST-3: Cover image is replaced if new image provided
/// - POST-4: Changes are immediately visible throughout the system
/// - POST-5: Success confirmation is displayed to staff member (at controller level)
/// </remarks>
public record UpdateCategoryCommand(int Id, UpdateCategoryDto CategoryDto) : IRequest<Result>;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var categoryRepository = _unitOfWork.Repository<Category>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();

            // PRE-1: Verify category exists
            var category = await categoryRepository.GetAsync(c => c.Id == request.Id);
            if (category == null)
                return Result.Failure($"Category with ID {request.Id} not found."); // UC038.E1: Category Not Found

            // Store original state for audit log
            var originalState = $"Name: {category.Name}, Description: {category.Description?.Substring(0, Math.Min(category.Description?.Length ?? 0, 100)) ?? "null"}{(category.Description?.Length > 100 ? "..." : "")}, HasCoverImage: {!string.IsNullOrEmpty(category.CoverImageUrl)}";

            // PRE-4: Validate input - name length (1-100 characters)
            if (string.IsNullOrWhiteSpace(request.CategoryDto.Name))
                return Result.Failure("Category name is required."); // UC038.E3: Invalid Category Information
                
            if (request.CategoryDto.Name.Length < 1 || request.CategoryDto.Name.Length > 100)
                return Result.Failure("Category name must be between 1 and 100 characters."); // UC038.E3: Invalid Category Information
            
            // PRE-4: Validate input - description length (max 500 characters)
            if (request.CategoryDto.Description != null && request.CategoryDto.Description.Length > 500)
                return Result.Failure("Category description must not exceed 500 characters."); // UC038.E3: Invalid Category Information

            // PRE-3: Check if name is changed and if new name already exists
            if (!string.Equals(category.Name, request.CategoryDto.Name, StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = await categoryRepository.ExistsAsync(c => 
                    c.Name.ToLower() == request.CategoryDto.Name.ToLower() && c.Id != request.Id);
                
                if (nameExists)
                    return Result.Failure($"Category with name '{request.CategoryDto.Name}' already exists."); // UC038.E2: Name Already Exists
            }

            // Update category properties
            category.Name = request.CategoryDto.Name;
            category.Description = request.CategoryDto.Description;
            category.CoverImageUrl = request.CategoryDto.CoverImageUrl;
            category.LastModifiedAt = DateTime.UtcNow;
            
            categoryRepository.Update(category);
            
            // Create detailed audit log entry for category update (BR-22)
            var newState = $"Name: {category.Name}, Description: {category.Description?.Substring(0, Math.Min(category.Description?.Length ?? 0, 100)) ?? "null"}{(category.Description?.Length > 100 ? "..." : "")}, HasCoverImage: {!string.IsNullOrEmpty(category.CoverImageUrl)}";
            
            var auditLog = new AuditLog
            {
                EntityType = "Category",
                EntityId = category.Id.ToString(),
                EntityName = category.Name,
                ActionType = AuditActionType.Update,
                Details = $"Updated category: {category.Name}",
                Module = "Category Management",
                BeforeState = originalState,
                AfterState = newState,
                IsSuccess = true
            };
            
            await auditRepository.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
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
                    EntityId = request.Id.ToString(),
                    ActionType = AuditActionType.Update,
                    Details = $"Failed to update category with ID {request.Id}",
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
            
            return Result.Failure($"Failed to update category: {ex.Message}"); // UC038.E5: System Update Error
        }
    }
}