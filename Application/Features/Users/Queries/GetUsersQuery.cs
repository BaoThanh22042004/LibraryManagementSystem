using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Users.Queries;

/// <summary>
/// Query to retrieve a paginated list of users with role-based filtering (UC007)
/// This implements the user list access functionality:
/// - Enforces BR-03 (User Information Access): Users can only view their own information; staff access is role-based
/// - Members cannot view other users
/// - Librarians can view all Members but not other Librarians or Admins
/// - Admins can view all users
/// </summary>
public record GetUsersQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null, int CurrentUserId = 0) : IRequest<PagedResult<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var userRepository = _unitOfWork.Repository<User>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Get current user to determine access permissions
        var currentUser = await userRepository.GetAsync(u => u.Id == request.CurrentUserId);
        if (currentUser == null)
        {
            // If no current user, return empty result
            return new PagedResult<UserDto>(new List<UserDto>(), 0, request.PageNumber, request.PageSize);
        }
        
        // Define the base predicate for search term filtering
        Expression<Func<User, bool>>? searchPredicate = null;
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            searchPredicate = u => u.FullName.Contains(request.SearchTerm) || 
                                  u.Email.Contains(request.SearchTerm);
        }
        
        // Apply role-based access control (BR-03)
        Expression<Func<User, bool>>? accessPredicate = null;
        
        if (currentUser.Role == UserRole.Member)
        {
            // Members can only see themselves
            accessPredicate = u => u.Id == request.CurrentUserId;
            
            // Log access attempt
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.AccessSensitiveData,
                EntityType = "User",
                Details = "Member accessed user list (filtered to self only)",
                Module = "UserManagement",
                IsSuccess = true
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (currentUser.Role == UserRole.Librarian)
        {
            // Librarians can see themselves and all Members
            accessPredicate = u => u.Id == request.CurrentUserId || u.Role == UserRole.Member;
            
            // Log access
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.AccessSensitiveData,
                EntityType = "User",
                Details = "Librarian accessed user list (filtered to self and members)",
                Module = "UserManagement",
                IsSuccess = true
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (currentUser.Role == UserRole.Admin)
        {
            // Admins can see all users
            accessPredicate = null; // No restriction
            
            // Log access
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.AccessSensitiveData,
                EntityType = "User",
                Details = "Admin accessed complete user list",
                Module = "UserManagement",
                IsSuccess = true
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        // Combine search and access predicates
        Expression<Func<User, bool>>? combinedPredicate = null;
        
        if (searchPredicate != null && accessPredicate != null)
        {
            // This is a simplification as combining expressions is more complex
            // In a real implementation, you would use a predicate builder or expression trees
            combinedPredicate = u => accessPredicate.Compile()(u) && searchPredicate.Compile()(u);
        }
        else if (searchPredicate != null)
        {
            combinedPredicate = searchPredicate;
        }
        else if (accessPredicate != null)
        {
            combinedPredicate = accessPredicate;
        }
        
        // Get paginated list of users
        var userQuery = await userRepository.PagedListAsync(
            pagedRequest,
            predicate: combinedPredicate,
            orderBy: q => q.OrderBy(u => u.FullName)
        );

        return new PagedResult<UserDto>(
            _mapper.Map<List<UserDto>>(userQuery.Items),
            userQuery.TotalCount,
            userQuery.PageNumber,
            userQuery.PageSize
        );
    }
}