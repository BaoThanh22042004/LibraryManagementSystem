using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services;

/// <summary>
/// Implementation of the user management service.
/// </summary>
public class UserService : IUserService
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;
	private readonly IAuthService _authService;
	private readonly IAuditService _auditService;
	private readonly ILogger<UserService> _logger;

	public UserService(
		IUnitOfWork unitOfWork,
		IMapper mapper,
		IAuthService authService,
		IAuditService auditService,
		ILogger<UserService> logger)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
		_authService = authService;
		_auditService = auditService;
		_logger = logger;
	}

	/// <inheritdoc/>
	public async Task<Result<int>> CreateUserAsync(CreateUserRequest request, int creatorId)
	{
		try
		{
			// Check if creator has permission to create this type of user
			var permissionCheck = await CheckUserPermissionForRoleCreationAsync(creatorId, request.Role);
			if (permissionCheck.IsFailure)
			{
				return Result.Failure<int>(permissionCheck.Error);
			}

			// Validate the password
			var passwordValidation = _authService.ValidatePassword(request.Password);
			if (passwordValidation.IsFailure)
			{
				return Result.Failure<int>(passwordValidation.Error);
			}

			await _unitOfWork.BeginTransactionAsync();

			// Check if email already exists
			var userRepo = _unitOfWork.Repository<User>();
			var existingUser = await userRepo.GetAsync(u => u.Email == request.Email);
			if (existingUser != null)
			{
				await _unitOfWork.RollbackTransactionAsync();
				return Result.Failure<int>("Email is already registered");
			}

			// Create user
			var user = _mapper.Map<User>(request);

			// Hash the password
			user.PasswordHash = PasswordHasher.HashPassword(request.Password);

			await userRepo.AddAsync(user);
			await _unitOfWork.SaveChangesAsync();

			// Create role-specific profile
			if (request.Role == UserRole.Member)
			{
				var memberRepo = _unitOfWork.Repository<Member>();
				var member = new Member
				{
					UserId = user.Id,
					MembershipNumber = !string.IsNullOrWhiteSpace(request.MembershipNumber)
						? request.MembershipNumber
						: GenerateMembershipNumber(),
					MembershipStartDate = DateTime.UtcNow,
					MembershipStatus = MembershipStatus.Active,
					OutstandingFines = 0
				};

				// Ensure membership number is unique
				while (await memberRepo.ExistsAsync(m => m.MembershipNumber == member.MembershipNumber))
				{
					member.MembershipNumber = GenerateMembershipNumber();
				}

				await memberRepo.AddAsync(member);
			}
			else if (request.Role == UserRole.Librarian)
			{
				var librarianRepo = _unitOfWork.Repository<Librarian>();
				var librarian = new Librarian
				{
					UserId = user.Id,
					EmployeeId = !string.IsNullOrWhiteSpace(request.EmployeeId)
						? request.EmployeeId
						: GenerateEmployeeId(),
					HireDate = DateTime.UtcNow
				};

				// Ensure employee ID is unique
				while (await librarianRepo.ExistsAsync(l => l.EmployeeId == librarian.EmployeeId))
				{
					librarian.EmployeeId = GenerateEmployeeId();
				}

				await librarianRepo.AddAsync(librarian);
			}

			await _unitOfWork.SaveChangesAsync();
			await _unitOfWork.CommitTransactionAsync();

			// Log user creation
			await _auditService.LogUserManagementAsync(
				creatorId,
				user.Id,
				AuditActionType.Create,
				true,
				$"Created new {user.Role} user: {user.FullName} ({user.Email})",
				null,
				JsonSerializer.Serialize(new { user.Id, user.Email, user.FullName, user.Role, user.Status }));

			return Result.Success(user.Id);
		}
		catch (Exception ex)
		{
			await _unitOfWork.RollbackTransactionAsync();
			_logger.LogError(ex, "Error creating user {Email} with role {Role}", request.Email, request.Role);
			return Result.Failure<int>("An error occurred while creating the user");
		}
	}

	/// <inheritdoc/>
	public async Task<Result<UserDetailsResponse>> GetUserDetailsAsync(int id, int requesterId)
	{
		try
		{
			// Check permission
			var permissionCheck = await CheckUserPermissionAsync(requesterId, id, UserAction.View);
			if (permissionCheck.IsFailure)
			{
				return Result.Failure<UserDetailsResponse>(permissionCheck.Error);
			}

			// Get user with role-specific details
			var userRepo = _unitOfWork.Repository<User>();
			var user = await userRepo.GetAsync(u => u.Id == id);

			if (user == null)
			{
				return Result.Failure<UserDetailsResponse>("User not found");
			}

			// Get member or librarian details if needed
			if (user.Role == UserRole.Member)
			{
				var memberRepo = _unitOfWork.Repository<Member>();
				user.Member = await memberRepo.GetAsync(m => m.UserId == user.Id);
			}
			else if (user.Role == UserRole.Librarian)
			{
				var librarianRepo = _unitOfWork.Repository<Librarian>();
				user.Librarian = await librarianRepo.GetAsync(l => l.UserId == user.Id);
			}

			// Map to response
			var response = _mapper.Map<UserDetailsResponse>(user);

			// Get additional information for Member
			if (user.Role == UserRole.Member && user.Member != null)
			{
				// Load loan and reservation counts
				var loanRepo = _unitOfWork.Repository<Loan>();
				var activeLoans = await loanRepo.CountAsync(l =>
					l.MemberId == user.Member.Id &&
					l.Status == LoanStatus.Active);

				var reservationRepo = _unitOfWork.Repository<Reservation>();
				var activeReservations = await reservationRepo.CountAsync(r =>
					r.MemberId == user.Member.Id &&
					r.Status == ReservationStatus.Active);

				if (response.MemberDetails != null)
				{
					response.MemberDetails.ActiveLoans = activeLoans;
					response.MemberDetails.ActiveReservations = activeReservations;
				}
			}

			// Log access
			if (requesterId != id)  // Don't log when users view their own profile
			{
				await _auditService.LogUserManagementAsync(
					requesterId,
					id,
					AuditActionType.AccessSensitiveData,
					true,
					$"User {requesterId} viewed details of user {id}");
			}

			return Result.Success(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving user details for ID {Id}", id);
			return Result.Failure<UserDetailsResponse>("An error occurred while retrieving user details");
		}
	}

	/// <inheritdoc/>
	public async Task<Result> UpdateUserAsync(UpdateUserRequest request, int updaterId)
	{
		try
		{
			// Check permission
			var permissionCheck = await CheckUserPermissionAsync(updaterId, request.Id, UserAction.Update);
			if (permissionCheck.IsFailure)
			{
				return Result.Failure(permissionCheck.Error);
			}

			// Get user
			var userRepo = _unitOfWork.Repository<User>();
			var user = await userRepo.GetAsync(u => u.Id == request.Id);

			if (user == null)
			{
				return Result.Failure("User not found");
			}

			// Capture before state for audit
			var beforeState = JsonSerializer.Serialize(new
			{
				user.FullName,
				user.Email,
				user.Phone,
				user.Address,
				user.Status
			});

			// Check if email is being changed
			var emailChanged = !string.IsNullOrEmpty(request.Email) &&
							   !request.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase);

			if (emailChanged)
			{
				// Check if new email is already in use
				var existingUser = await userRepo.GetAsync(u =>
					u.Id != request.Id &&
					u.Email == request.Email);

				if (existingUser != null)
				{
					return Result.Failure("Email is already in use by another user");
				}
			}

			// Update basic properties
			user.FullName = request.FullName;
			if (emailChanged)
			{
				user.Email = request.Email;
			}
			user.Phone = request.Phone;
			user.Address = request.Address;
			user.LastModifiedAt = DateTime.UtcNow;

			// Update status if provided and changed
			if (request.Status.HasValue && request.Status.Value != user.Status)
			{
				// Check if status change reason is provided (required for status changes)
				if (string.IsNullOrWhiteSpace(request.StatusChangeReason) && updaterId != request.Id)
				{
					return Result.Failure("Status change reason is required");
				}

				user.Status = request.Status.Value;
			}

			userRepo.Update(user);
			await _unitOfWork.SaveChangesAsync();

			// Capture after state for audit
			var afterState = JsonSerializer.Serialize(new
			{
				user.FullName,
				user.Email,
				user.Phone,
				user.Address,
				user.Status
			});

			// Log update
			await _auditService.LogUserManagementAsync(
				updaterId,
				request.Id,
				AuditActionType.Update,
				true,
				!string.IsNullOrWhiteSpace(request.StatusChangeReason)
					? $"Updated user {request.Id} with status change reason: {request.StatusChangeReason}"
					: $"Updated user {request.Id}",
				beforeState,
				afterState);

			return Result.Success();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating user {Id}", request.Id);
			return Result.Failure("An error occurred while updating the user");
		}
	}

	/// <inheritdoc/>
	public async Task<Result> DeleteUserAsync(int id, int deleterId)
	{
		try
		{
			// Check permission
			var permissionCheck = await CheckUserPermissionAsync(deleterId, id, UserAction.Delete);
			if (permissionCheck.IsFailure)
			{
				return Result.Failure(permissionCheck.Error);
			}

			// Get user
			var userRepo = _unitOfWork.Repository<User>();
			var user = await userRepo.GetAsync(u => u.Id == id);

			if (user == null)
			{
				return Result.Failure("User not found");
			}

			await _unitOfWork.BeginTransactionAsync();

			// Get role-specific data
			if (user.Role == UserRole.Member)
			{
				var memberRepo = _unitOfWork.Repository<Member>();
				var member = await memberRepo.GetAsync(m => m.UserId == user.Id);

				if (member != null)
				{
					// Check if member can be deleted
					var deletionCheck = await CanDeleteMemberAsync(member.Id);
					if (!deletionCheck.IsSuccess || !deletionCheck.Value.CanDelete)
					{
						await _unitOfWork.RollbackTransactionAsync();
						return Result.Failure(deletionCheck.IsSuccess
							? deletionCheck.Value.Message
							: "Failed to check member deletion eligibility");
					}

					// Delete member
					memberRepo.Delete(member);
				}
			}
			else if (user.Role == UserRole.Librarian)
			{
				var librarianRepo = _unitOfWork.Repository<Librarian>();
				var librarian = await librarianRepo.GetAsync(l => l.UserId == user.Id);

				if (librarian != null)
				{
					// Here you could add logic to check for any active responsibilities
					// For example, assigned tasks, active management duties, etc.

					// Delete librarian
					librarianRepo.Delete(librarian);
				}
			}

			// Capture user info for audit log
			var userInfo = new
			{
				user.Id,
				user.FullName,
				user.Email,
				user.Role
			};

			// Delete user
			userRepo.Delete(user);
			await _unitOfWork.SaveChangesAsync();
			await _unitOfWork.CommitTransactionAsync();

			// Log deletion
			await _auditService.LogUserManagementAsync(
				deleterId,
				id,
				AuditActionType.Delete,
				true,
				$"Deleted {user.Role} user: {user.FullName} ({user.Email})",
				JsonSerializer.Serialize(userInfo));

			return Result.Success();
		}
		catch (Exception ex)
		{
			await _unitOfWork.RollbackTransactionAsync();
			_logger.LogError(ex, "Error deleting user {Id}", id);
			return Result.Failure("An error occurred while deleting the user");
		}
	}

	/// <inheritdoc/>
	public async Task<Result<UserSearchResponse>> SearchUsersAsync(UserSearchRequest request, int requesterId)
	{
		try
		{
			// Get requester role
			var requesterRole = await GetUserRoleAsync(requesterId);
			if (requesterRole == null)
			{
				return Result.Failure<UserSearchResponse>("User not found");
			}

			// Validate search permissions based on role
			var userRepo = _unitOfWork.Repository<User>();
			var query = userRepo.Query();

			// Apply role-based filtering
			if (requesterRole == UserRole.Member)
			{
				// Members can only see their own profile
				query = query.Where(u => u.Id == requesterId);
			}
			else if (requesterRole == UserRole.Librarian)
			{
				// Librarians can see Members and themselves
				query = query.Where(u => u.Role == UserRole.Member || u.Id == requesterId);
			}
			// Admins can see everyone, so no additional filtering needed

			// Apply search filters
			if (request.Role.HasValue)
			{
				query = query.Where(u => u.Role == request.Role.Value);
			}

			if (request.Status.HasValue)
			{
				query = query.Where(u => u.Status == request.Status.Value);
			}

			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm;
				query = query.Where(u =>
					u.FullName.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
					u.Email.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
					(u.Phone != null && u.Phone.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)));

				// Note: The following complex conditions would be handled in the database query
				// This is a simplified version for the repository pattern
			}

			// Create paged request
			var pagedRequest = new PagedRequest
			{
				Page = request.PageNumber,
				PageSize = request.PageSize
			};

			// Execute query with paging
			var pagedResult = await userRepo.PagedListAsync(
				pagedRequest,
				null, // predicate is applied directly in the query
				query => query.OrderBy(u => u.Id),
				true); // asNoTracking

			// Load member and librarian data for users
			var users = pagedResult.Items.ToList();
			var userIds = users.Select(u => u.Id).ToList();

			var memberRepo = _unitOfWork.Repository<Member>();
			var members = await memberRepo.ListAsync(m => userIds.Contains(m.UserId));

			var librarianRepo = _unitOfWork.Repository<Librarian>();
			var librarians = await librarianRepo.ListAsync(l => userIds.Contains(l.UserId));

			// Associate members and librarians with users
			foreach (var user in users)
			{
				if (user.Role == UserRole.Member)
				{
					user.Member = members.FirstOrDefault(m => m.UserId == user.Id);
				}
				else if (user.Role == UserRole.Librarian)
				{
					user.Librarian = librarians.FirstOrDefault(l => l.UserId == user.Id);
				}
			}

			// Map to response DTOs
			var response = new UserSearchResponse
			{
				Items = _mapper.Map<List<UserSummaryDto>>(users),
				Count = pagedResult.Count,
				Page = pagedResult.Page,
				PageSize = pagedResult.PageSize
			};

			// Log search if not done by a member (to avoid excessive logging)
			if (requesterRole != UserRole.Member)
			{
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = requesterId,
					ActionType = AuditActionType.AccessSensitiveData,
					EntityType = "User",
					EntityId = null, // Multiple users
					Details = $"Searched users with filters: Role={request.Role}, Status={request.Status}, Term={request.SearchTerm}",
					IsSuccess = true
				});
			}

			return Result.Success(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error searching users with request {@Request}", request);
			return Result.Failure<UserSearchResponse>("An error occurred while searching users");
		}
	}

	/// <inheritdoc/>
	public async Task<Result> CheckUserPermissionAsync(int actorId, int targetId, UserAction action)
	{
		try
		{
			// Get actor and target
			var userRepo = _unitOfWork.Repository<User>();
			var actor = await userRepo.GetAsync(u => u.Id == actorId);

			if (actor == null)
			{
				return Result.Failure("Actor user not found");
			}

			// Self-access is always allowed for View actions
			if (actorId == targetId && action == UserAction.View)
			{
				return Result.Success();
			}

			// Self-update is allowed
			if (actorId == targetId && action == UserAction.Update)
			{
				return Result.Success();
			}

			// For all other actions and access to other users, check role permissions
			var target = await userRepo.GetAsync(u => u.Id == targetId);

			if (target == null)
			{
				return Result.Failure("Target user not found");
			}

			// Apply role-based permission checks
			switch (actor.Role)
			{
				case UserRole.Member:
					// Members can only view/update themselves
					return Result.Failure("Insufficient permissions");

				case UserRole.Librarian:
					// Librarians can manage Members but not other Librarians or Admins
					if (target.Role != UserRole.Member)
					{
						return Result.Failure("Librarians can only manage Member accounts");
					}

					// Librarians can perform all actions on Members
					return Result.Success();

				case UserRole.Admin:
					// Admins can do anything
					return Result.Success();

				default:
					return Result.Failure("Unknown role");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking user permission: Actor {ActorId}, Target {TargetId}, Action {Action}",
				actorId, targetId, action);
			return Result.Failure("An error occurred while checking permissions");
		}
	}

	/// <inheritdoc/>
	public async Task<Result> CheckUserRoleAsync(int userId, UserRole requiredRole)
	{
		try
		{
			var userRole = await GetUserRoleAsync(userId);

			if (userRole == null)
			{
				return Result.Failure("User not found");
			}

			// Check if user's role meets or exceeds the required role
			// Role hierarchy: Admin > Librarian > Member
			if ((int)userRole >= (int)requiredRole)
			{
				return Result.Success();
			}

			return Result.Failure($"User does not have required role. Required: {requiredRole}, Actual: {userRole}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking user role for user {UserId}", userId);
			return Result.Failure("An error occurred while checking user role");
		}
	}

	/// <inheritdoc/>
	public async Task<Result<MemberDeletionValidationResult>> CanDeleteMemberAsync(int memberId)
	{
		try
		{
			var memberRepo = _unitOfWork.Repository<Member>();
			var member = await memberRepo.GetAsync(m => m.Id == memberId);

			if (member == null)
			{
				return Result.Failure<MemberDeletionValidationResult>("Member not found");
			}

			var result = new MemberDeletionValidationResult
			{
				CanDelete = true,
				HasActiveLoans = false,
				HasActiveReservations = false,
				HasUnpaidFines = false,
				Message = "Member can be deleted"
			};

			// Check for active loans
			var loanRepo = _unitOfWork.Repository<Loan>();
			var activeLoans = await loanRepo.CountAsync(l =>
				l.MemberId == memberId &&
				l.Status == LoanStatus.Active);

			if (activeLoans > 0)
			{
				result.HasActiveLoans = true;
				result.CanDelete = false;
				result.Message = "Cannot delete member with active loans";
			}

			// Check for active reservations
			var reservationRepo = _unitOfWork.Repository<Reservation>();
			var activeReservations = await reservationRepo.CountAsync(r =>
				r.MemberId == memberId &&
				r.Status == ReservationStatus.Active);

			if (activeReservations > 0)
			{
				result.HasActiveReservations = true;
				result.CanDelete = false;
				result.Message = result.HasActiveLoans
					? result.Message + " and active reservations"
					: "Cannot delete member with active reservations";
			}

			// Check for unpaid fines
			if (member.OutstandingFines > 0)
			{
				result.HasUnpaidFines = true;
				result.CanDelete = false;
				result.Message = result.HasActiveLoans || result.HasActiveReservations
					? result.Message + " and unpaid fines"
					: "Cannot delete member with unpaid fines";
			}

			return Result.Success(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking if member {MemberId} can be deleted", memberId);
			return Result.Failure<MemberDeletionValidationResult>("An error occurred while validating member deletion");
		}
	}

	#region Helper Methods

	private async Task<Result> CheckUserPermissionForRoleCreationAsync(int creatorId, UserRole roleToCreate)
	{
		var userRepo = _unitOfWork.Repository<User>();
		var creator = await userRepo.GetAsync(u => u.Id == creatorId);

		if (creator == null)
		{
			return Result.Failure("Creator user not found");
		}

		switch (creator.Role)
		{
			case UserRole.Member:
				// Members cannot create any users
				return Result.Failure("Members cannot create user accounts");

			case UserRole.Librarian:
				// Librarians can only create Members
				if (roleToCreate != UserRole.Member)
				{
					return Result.Failure("Librarians can only create Member accounts");
				}
				return Result.Success();

			case UserRole.Admin:
				// Admins can create any type of user
				return Result.Success();

			default:
				return Result.Failure("Unknown role");
		}
	}

	private async Task<UserRole?> GetUserRoleAsync(int userId)
	{
		var userRepo = _unitOfWork.Repository<User>();
		var user = await userRepo.GetAsync(u => u.Id == userId);
		return user?.Role;
	}

	private string GenerateMembershipNumber()
	{
		// Generate a random membership number with format: LIB-YYYYMMDD-XXXX
		// Where YYYYMMDD is the current date and XXXX is a random number
		var date = DateTime.UtcNow.ToString("yyyyMMdd");
		var random = new Random();
		var randomPart = random.Next(1000, 10000).ToString();

		return $"LIB-{date}-{randomPart}";
	}

	private string GenerateEmployeeId()
	{
		// Generate a random employee ID with format: EMP-YYYYMM-XXXX
		// Where YYYYMM is the current year and month and XXXX is a random number
		var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
		var random = new Random();
		var randomPart = random.Next(1000, 10000).ToString();

		return $"EMP-{yearMonth}-{randomPart}";
	}

	#endregion
}
