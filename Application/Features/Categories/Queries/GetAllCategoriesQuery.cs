using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Categories.Queries;

/// <summary>
/// Query to retrieve a complete list of all categories (UC041 - Browse Categories, alternative flow 41.2).
/// </summary>
/// <remarks>
/// Implements UC041 specifications (Complete Category List variant) with the following requirements:
/// - PRE-1: System must be accessible (enforced at controller level)
/// - PRE-2: System must have category data available for browsing
/// 
/// Ensures the following postconditions:
/// - POST-1: All categories are displayed in a complete list (non-paginated)
/// - POST-3: Categories are ordered alphabetically for consistent browsing
/// - POST-5: Book count is displayed for each category
/// </remarks>
public record GetAllCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, List<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var categoryRepository = _unitOfWork.Repository<Category>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // PRE-2: Retrieve all categories ordered alphabetically with book counts
            var categories = await categoryRepository.ListAsync(
                orderBy: q => q.OrderBy(c => c.Name), // POST-3: Alphabetical ordering
                includes: new System.Linq.Expressions.Expression<Func<Category, object>>[] { c => c.Books }
            );

            // Check for UC041.E3: Large Dataset Performance Warning
            if (categories.Count > 500) // Arbitrary threshold for demonstration
            {
                // Log potential performance issue
                await auditRepository.AddAsync(new AuditLog
                {
                    EntityType = "Category",
                    ActionType = AuditActionType.AccessSensitiveData,
                    Details = $"Retrieved large non-paginated category list ({categories.Count} items)",
                    Module = "Category Management",
                    IsSuccess = true,
                    ErrorMessage = "Performance warning: Large dataset requested without pagination"
                });
            }
            else
            {
                // Log standard access
                await auditRepository.AddAsync(new AuditLog
                {
                    EntityType = "Category",
                    ActionType = AuditActionType.AccessSensitiveData,
                    Details = $"Retrieved complete category list ({categories.Count} items)",
                    Module = "Category Management",
                    IsSuccess = true
                });
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return mapped categories with book counts
            return _mapper.Map<List<CategoryDto>>(categories);
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
                    ActionType = AuditActionType.AccessSensitiveData,
                    Details = "Failed to retrieve complete category list",
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
            
            // Return empty list on error
            return new List<CategoryDto>();
        }
    }
}