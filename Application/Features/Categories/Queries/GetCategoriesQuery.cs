using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Categories.Queries;

/// <summary>
/// Query to retrieve a paginated list of categories with optional search functionality (UC041 - Browse Categories).
/// </summary>
/// <remarks>
/// Implements UC041 specifications with the following requirements:
/// - PRE-1: System must be accessible (enforced at controller level)
/// - PRE-2: System must have category data available for browsing
/// - PRE-3: Pagination parameters must be valid if specified
/// - PRE-4: Search terms must be properly formatted if provided
/// 
/// Ensures the following postconditions:
/// - POST-1: Categories are displayed according to requested format (paginated)
/// - POST-2: Search filtering is applied if search terms provided
/// - POST-3: Categories are ordered alphabetically for consistent browsing
/// - POST-4: Total count and pagination information are shown
/// - POST-5: Book count is displayed for each category
/// </remarks>
public record GetCategoriesQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // PRE-3: Validate and normalize pagination parameters
            int pageNumber = Math.Max(request.PageNumber, 1);
            int pageSize = Math.Clamp(request.PageSize, 1, 100);
            
            var pagedRequest = new PagedRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var categoryRepository = _unitOfWork.Repository<Category>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // PRE-4: Process search term if provided (case-insensitive search)
            Expression<Func<Category, bool>>? searchPredicate = null;
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                searchPredicate = c => 
                    c.Name.ToLower().Contains(request.SearchTerm.ToLower()) || 
                    (c.Description != null && c.Description.ToLower().Contains(request.SearchTerm.ToLower()));
            }
            
            // POST-3: Order alphabetically by name
            var categoryQuery = await categoryRepository.PagedListAsync(
                pagedRequest,
                predicate: searchPredicate,
                orderBy: q => q.OrderBy(c => c.Name),
                includes: new Expression<Func<Category, object>>[] { c => c.Books }
            );

            // Log query for audit purposes (BR-22)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Category",
                ActionType = AuditActionType.AccessSensitiveData,
                Details = $"Retrieved categories list (Page: {pageNumber}, Size: {pageSize}, SearchTerm: {(string.IsNullOrEmpty(request.SearchTerm) ? "none" : request.SearchTerm)})",
                Module = "Category Management",
                IsSuccess = true
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // POST-1, POST-2, POST-4, POST-5: Return properly formatted result
            return new PagedResult<CategoryDto>(
                _mapper.Map<List<CategoryDto>>(categoryQuery.Items), // Includes book count in mapping
                categoryQuery.TotalCount,
                categoryQuery.PageNumber,
                categoryQuery.PageSize
            );
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
                    Details = $"Failed to retrieve categories list",
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
            
            // Return empty result on error
            return new PagedResult<CategoryDto>(
                new List<CategoryDto>(),
                0,
                request.PageNumber,
                request.PageSize
            );
        }
    }
}