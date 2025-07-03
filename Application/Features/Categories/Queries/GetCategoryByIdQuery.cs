using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Categories.Queries;

/// <summary>
/// Query to retrieve detailed information about a specific category (UC040 - View Category Details).
/// </summary>
/// <remarks>
/// Implements UC040 specifications with the following requirements:
/// - PRE-1: Category must exist in the system
/// - PRE-2: System must be accessible (enforced at controller level)
/// - PRE-3: System must be able to retrieve category and associated book information
/// - PRE-4: Category information must be current and accessible
/// 
/// Ensures the following postconditions:
/// - POST-1: Complete category details are displayed to user
/// - POST-2: List of books assigned to category is shown
/// - POST-3: Category metadata including name, description, and cover image are visible
/// - POST-4: Book count and category statistics are available
/// - POST-5: User can navigate to individual book details from category view (handled at Web layer)
/// </remarks>
public record GetCategoryByIdQuery(int Id) : IRequest<CategoryDetailsDto?>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCategoryByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CategoryDetailsDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var categoryRepository = _unitOfWork.Repository<Category>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // PRE-1, PRE-3: Retrieve category with its associated books
            var category = await categoryRepository.GetAsync(
                c => c.Id == request.Id,
                c => c.Books
            );

            // PRE-4: Check if category exists
            if (category == null)
            {
                // Log access attempt to non-existent category
                await auditRepository.AddAsync(new AuditLog
                {
                    EntityType = "Category",
                    EntityId = request.Id.ToString(),
                    ActionType = AuditActionType.AccessSensitiveData,
                    Details = $"Attempted to view non-existent category with ID {request.Id}",
                    Module = "Category Management",
                    IsSuccess = false,
                    ErrorMessage = "Category not found"
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                return null; // UC040.E1: Category Not Found
            }

            // Log successful access for audit purposes (BR-22)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Category",
                EntityId = category.Id.ToString(),
                EntityName = category.Name,
                ActionType = AuditActionType.AccessSensitiveData,
                Details = $"Viewed category details: {category.Name} (Books count: {category.Books.Count})",
                Module = "Category Management",
                IsSuccess = true
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // POST-1, POST-2, POST-3, POST-4: Return detailed category information
            return _mapper.Map<CategoryDetailsDto>(category);
        }
        catch (Exception ex)
        {
            // Log exception
            try
            {
                var auditRepository = _unitOfWork.Repository<AuditLog>();
                await auditRepository.AddAsync(new AuditLog
                {
                    EntityType = "Category",
                    EntityId = request.Id.ToString(),
                    ActionType = AuditActionType.AccessSensitiveData,
                    Details = $"Failed to retrieve category details for ID {request.Id}",
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
            
            return null; // UC040.E1: Category Access Error
        }
    }
}