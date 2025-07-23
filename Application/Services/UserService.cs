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
	private readonly ILogger<UserService> _logger;

	public UserService(
		IUnitOfWork unitOfWork,
		IMapper mapper,
		IAuthService authService,
		ILogger<UserService> logger)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
		_authService = authService;
		_logger = logger;
	}

	/// <inheritdoc/>
	public async Task<Result<int>> CreateUserAsync(CreateUserRequest request)
	{
		try
		{
			// Block creation of Admin users via UI
			if (request.Role == UserRole.Admin)
			{
				return Result.Failure<int>("Creating Admin users via the UI is not allowed.");
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
				string membershipNumber = !string.IsNullOrWhiteSpace(request.MembershipNumber)
					? request.MembershipNumber
					: GenerateMembershipNumber();

				// If custom membership number is provided and duplicate, return error
				if (!string.IsNullOrWhiteSpace(request.MembershipNumber))
				{
					if (await memberRepo.ExistsAsync(m => m.MembershipNumber == membershipNumber))
					{
						await _unitOfWork.RollbackTransactionAsync();
						return Result.Failure<int>("Membership number already exists");
					}
				}
				else
				{
					// Auto-generate until unique
					while (await memberRepo.ExistsAsync(m => m.MembershipNumber == membershipNumber))
					{
						membershipNumber = GenerateMembershipNumber();
					}
				}

				var member = new Member
				{
					UserId = user.Id,
					MembershipNumber = membershipNumber,
					MembershipStartDate = DateTime.UtcNow,
					MembershipStatus = MembershipStatus.Active,
					OutstandingFines = 0
				};
				await memberRepo.AddAsync(member);
			}
			else if (request.Role == UserRole.Librarian)
			{
				var librarianRepo = _unitOfWork.Repository<Librarian>();
				string employeeId = !string.IsNullOrWhiteSpace(request.EmployeeId)
					? request.EmployeeId
					: GenerateEmployeeId();

				// If custom employee ID is provided and duplicate, return error
				if (!string.IsNullOrWhiteSpace(request.EmployeeId))
				{
					if (await librarianRepo.ExistsAsync(l => l.EmployeeId == employeeId))
					{
						await _unitOfWork.RollbackTransactionAsync();
						return Result.Failure<int>("Employee ID already exists");
					}
				}
				else
				{
					// Auto-generate until unique
					while (await librarianRepo.ExistsAsync(l => l.EmployeeId == employeeId))
					{
						employeeId = GenerateEmployeeId();
					}
				}

				var librarian = new Librarian
				{
					UserId = user.Id,
					EmployeeId = employeeId,
					HireDate = DateTime.UtcNow
				};
				await librarianRepo.AddAsync(librarian);
			}

			await _unitOfWork.SaveChangesAsync();
			await _unitOfWork.CommitTransactionAsync();

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
	public async Task<Result<UserDetailsDto>> GetUserDetailsAsync(int id)
	{
		try
		{
			// Get user with role-specific details
			var userRepo = _unitOfWork.Repository<User>();
			var user = await userRepo.GetAsync(u => u.Id == id);

			if (user == null)
			{
				return Result.Failure<UserDetailsDto>("User not found");
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
			var response = _mapper.Map<UserDetailsDto>(user);

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

			return Result.Success(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving user details for ID {Id}", id);
			return Result.Failure<UserDetailsDto>("An error occurred while retrieving user details");
		}
	}

    /// <inheritdoc/>
    public async Task<Result<UserDetailsDto>> GetUserDetailsByEmailAsync(string email)
    {
        try
        {
            // Get user with role-specific details
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetAsync(u => u.Email == email);

            if (user == null)
            {
                return Result.Failure<UserDetailsDto>("User not found");
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
            var response = _mapper.Map<UserDetailsDto>(user);

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

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user details for ID {Id}", email);
            return Result.Failure<UserDetailsDto>("An error occurred while retrieving user details");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> UpdateUserAsync(UpdateUserRequest request)
	{
		try
		{
			// Get user
			var userRepo = _unitOfWork.Repository<User>();
			var user = await userRepo.GetAsync(u => u.Id == request.Id);

			if (user == null)
			{
				return Result.Failure("User not found");
			}

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
				user.Status = request.Status.Value;
			}

			userRepo.Update(user);
			await _unitOfWork.SaveChangesAsync();

			return Result.Success();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating user {Id}", request.Id);
			return Result.Failure("An error occurred while updating the user");
		}
	}

	/// <inheritdoc/>
	public async Task<Result> DeleteUserAsync(int id)
	{
		try
		{
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

			// Delete user
			userRepo.Delete(user);
			await _unitOfWork.SaveChangesAsync();
			await _unitOfWork.CommitTransactionAsync();

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
	public async Task<Result<PagedResult<UserBasicDto>>> SearchUsersAsync(UserSearchRequest request)
	{
		try
		{
			// Validate search permissions based on role
			var userRepo = _unitOfWork.Repository<User>();
			var query = userRepo.Query();

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
				query = query.Where(u =>
					u.FullName.Contains(request.SearchTerm, StringComparison.CurrentCultureIgnoreCase) ||
					u.Email.Contains(request.SearchTerm, StringComparison.CurrentCultureIgnoreCase) ||
					(u.Phone != null && u.Phone.Contains(request.SearchTerm, StringComparison.CurrentCultureIgnoreCase)));

				// Note: The following complex conditions would be handled in the database query
				// This is a simplified version for the repository pattern
			}

			// Execute query with paging
			var pagedResult = await userRepo.PagedListAsync(
				request,
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
			var response = new PagedResult<UserBasicDto>
			{
				Items = _mapper.Map<List<UserBasicDto>>(users),
				Count = pagedResult.Count,
				Page = pagedResult.Page,
				PageSize = pagedResult.PageSize
			};

			return Result.Success(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error searching users with request {@Request}", request);
			return Result.Failure<PagedResult<UserBasicDto>>("An error occurred while searching users");
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
	public async Task<Result<MemberDeletionValidationDto>> CanDeleteMemberAsync(int memberId)
	{
		try
		{
			var memberRepo = _unitOfWork.Repository<Member>();
			var member = await memberRepo.GetAsync(m => m.Id == memberId);

			if (member == null)
			{
				return Result.Failure<MemberDeletionValidationDto>("Member not found");
			}

			var result = new MemberDeletionValidationDto
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
			return Result.Failure<MemberDeletionValidationDto>("An error occurred while validating member deletion");
		}
	}

	public async Task<Result<UserDetailsDto>> GetUserDetailsByMemberIdAsync(int memberId)
	{
		try
		{
			var memberRepo = _unitOfWork.Repository<Member>();
			var member = await memberRepo.GetAsync(m => m.Id == memberId);
			if (member == null)
			{
				return Result.Failure<UserDetailsDto>("Member not found");
			}
			var userRepo = _unitOfWork.Repository<User>();
			var user = await userRepo.GetAsync(u => u.Id == member.UserId);
			if (user == null)
			{
				return Result.Failure<UserDetailsDto>("User not found for the given member");
			}
			var userDetails = _mapper.Map<UserDetailsDto>(user);
			userDetails.MemberDetails = _mapper.Map<MemberDetailsDto>(member);
			return Result.Success(userDetails);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving user details for member {MemberId}", memberId);
			return Result.Failure<UserDetailsDto>("An error occurred while retrieving user details by member ID");
		}
	}

	#region Helper Methods

	private async Task<UserRole?> GetUserRoleAsync(int userId)
	{
		var userRepo = _unitOfWork.Repository<User>();
		var user = await userRepo.GetAsync(u => u.Id == userId);
		return user?.Role;
	}

	private static string GenerateMembershipNumber()
	{
		// Generate a random membership number with format: LIB-YYYYMMDD-XXXX
		// Where YYYYMMDD is the current date and XXXX is a random number
		var date = DateTime.UtcNow.ToString("yyyyMMdd");
		var random = new Random();
		var randomPart = random.Next(1000, 10000).ToString();

		return $"LIB-{date}-{randomPart}";
	}

	private static string GenerateEmployeeId()
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
