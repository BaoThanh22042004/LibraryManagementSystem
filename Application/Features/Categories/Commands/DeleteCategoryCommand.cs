using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Categories.Commands;

/// <summary>
/// Command to delete a category (UC039 - Delete Category).
/// </summary>
/// <remarks>
/// Implements UC039 specifications with the following requirements:
/// - PRE-1: Category must exist in the system
/// - PRE-2: Staff member must have category deletion permissions (enforced at controller level)
/// - PRE-3: Category must not have any books currently assigned to it
/// - PRE-4: Category must not have any dependent relationships in the system
/// 
/// Ensures the following postconditions:
/// - POST-1: Category is permanently removed from the system
/// - POST-2: Category no longer appears in any category lists or selection interfaces
/// - POST-3: System maintains data integrity with no orphaned references
/// - POST-4: Staff receives confirmation of successful deletion
/// - POST-5: Category management interface is updated to reflect removal
/// </remarks>
public record DeleteCategoryCommand(int Id) : IRequest<Result>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var categoryRepository = _unitOfWork.Repository<Category>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();

            // PRE-1: Verify category exists
            var category = await categoryRepository.GetAsync(
                c => c.Id == request.Id,
                c => c.Books // Include books to check dependencies
            );
            
            if (category == null)
                return Result.Failure($"Category with ID {request.Id} not found."); // UC039.E1: Category Not Found

            // Store info for audit log
            var categoryName = category.Name;
            var categoryState = $"Name: {category.Name}, Description: {category.Description?.Substring(0, Math.Min(category.Description?.Length ?? 0, 100)) ?? "null"}{(category.Description?.Length > 100 ? "..." : "")}, HasCoverImage: {!string.IsNullOrEmpty(category.CoverImageUrl)}";

            // PRE-3: Check if the category has any associated books
            if (category.Books.Any())
            {
                var bookCount = category.Books.Count;
                return Result.Failure(
                    $"Cannot delete category '{category.Name}' because it has {bookCount} associated book{(bookCount > 1 ? "s" : "")}. " +
                    "Please reassign the books to other categories first."
                ); // UC039.E2: Category Has Assigned Books
            }

            // PRE-4: Check for any other system dependencies (if any)
            // Note: In the current model, only books can depend on categories
            
            // Delete the category
            categoryRepository.Delete(category);
            
            // Create audit log entry for category deletion (BR-22)
            var auditLog = new AuditLog
            {
                EntityType = "Category",
                EntityId = request.Id.ToString(),
                EntityName = categoryName,
                ActionType = AuditActionType.Delete,
                Details = $"Deleted category: {categoryName}",
                Module = "Category Management",
                BeforeState = categoryState,
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
                    ActionType = AuditActionType.Delete,
                    Details = $"Failed to delete category with ID {request.Id}",
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
            
            return Result.Failure($"Failed to delete category: {ex.Message}"); // UC039.E5: System Deletion Error
        }
    }
}