using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

/// <summary>
/// Command to delete a user account (UC009)
/// This implements the user deletion functionality:
/// - Enforces role-based permissions (BR-01): Only Admin or Librarian can delete users
/// - Librarians can only delete Member accounts; Admins can delete both Librarian and Member accounts
/// - Prevents deletion if user has active obligations (BR-23)
/// - Maintains audit trail (BR-22)
/// </summary>
public record DeleteUserCommand(int Id, int CurrentUserId) : IRequest<Result>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var memberRepository = _unitOfWork.Repository<Member>();
        var librarianRepository = _unitOfWork.Repository<Librarian>();
        var auditLogRepository = _unitOfWork.Repository<AuditLog>();
        
        // Verify current user has permission to delete users (BR-01)
        var currentUser = await userRepository.GetAsync(u => u.Id == request.CurrentUserId);
        if (currentUser == null)
        {
            return Result.Failure("Current user not found.");
        }
        
        if (currentUser.Role != UserRole.Admin && currentUser.Role != UserRole.Librarian)
        {
            // Log unauthorized deletion attempt (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.Delete,
                EntityType = "User",
                EntityId = request.Id.ToString(),
                Details = $"Unauthorized user deletion attempt by user {request.CurrentUserId}",
                Module = "UserManagement",
                IsSuccess = false,
                ErrorMessage = "Insufficient permissions"
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure("Only administrators and librarians can delete user accounts.");
        }
        
        // Get user
        var user = await userRepository.GetAsync(
            u => u.Id == request.Id,
            u => u.Member!,
            u => u.Librarian!,
            u => u.Notifications
        );
        
        if (user == null)
        {
            return Result.Failure($"User with ID {request.Id} not found.");
        }
        
        // Check for valid deletion permissions based on roles (BR-01)
        if (currentUser.Role == UserRole.Librarian)
        {
            // Librarians can only delete Members, not other Librarians or Admins (UC009 Exception 9.0.E2)
            if (user.Role != UserRole.Member)
            {
                // Log unauthorized deletion attempt (BR-22)
                await auditLogRepository.AddAsync(new AuditLog
                {
                    UserId = request.CurrentUserId,
                    ActionType = AuditActionType.Delete,
                    EntityType = "User",
                    EntityId = user.Id.ToString(),
                    EntityName = user.FullName,
                    Details = $"Librarian attempted to delete non-member user {user.Email}",
                    Module = "UserManagement",
                    IsSuccess = false,
                    ErrorMessage = "Librarians can only delete member accounts"
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                return Result.Failure("Librarians can only delete member accounts.");
            }
        }
        
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // First validate the deletion using the ValidateUserDeletionCommand logic
            // BR-23: Members cannot be deleted if they have active loans, reservations, or unpaid fines
            if (user.Member != null)
            {
                // Check if member has active loans or reservations
                var member = await memberRepository.GetAsync(
                    m => m.Id == user.Member.Id,
                    m => m.Loans,
                    m => m.Reservations,
                    m => m.Fines
                );
                
                if (member != null)
                {
                    // UC009 Exception 9.0.E3: Member Has Active Loans
                    var hasActiveLoans = member.Loans.Any(l => l.Status == LoanStatus.Active);
                    if (hasActiveLoans)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        
                        // Log failed deletion due to active loans (BR-22)
                        await auditLogRepository.AddAsync(new AuditLog
                        {
                            UserId = request.CurrentUserId,
                            ActionType = AuditActionType.Delete,
                            EntityType = "User",
                            EntityId = user.Id.ToString(),
                            EntityName = user.FullName,
                            Details = $"Failed to delete user {user.Email} - has active loans",
                            Module = "UserManagement",
                            IsSuccess = false,
                            ErrorMessage = "User has active loans"
                        });
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        return Result.Failure("Cannot delete user with active loans. Please return all books first.");
                    }
                    
                    // UC009 Exception 9.0.E4: Member Has Active Reservations
                    var hasActiveReservations = member.Reservations.Any(r => r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled);
                    if (hasActiveReservations)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        
                        // Log failed deletion due to active reservations (BR-22)
                        await auditLogRepository.AddAsync(new AuditLog
                        {
                            UserId = request.CurrentUserId,
                            ActionType = AuditActionType.Delete,
                            EntityType = "User",
                            EntityId = user.Id.ToString(),
                            EntityName = user.FullName,
                            Details = $"Failed to delete user {user.Email} - has active reservations",
                            Module = "UserManagement",
                            IsSuccess = false,
                            ErrorMessage = "User has active reservations"
                        });
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        return Result.Failure("Cannot delete user with active reservations. Please cancel all reservations first.");
                    }
                    
                    // UC009 Exception 9.0.E5: Member Has Unpaid Fines
                    var hasUnpaidFines = member.Fines.Any(f => f.Status == FineStatus.Pending);
                    if (hasUnpaidFines)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        
                        // Log failed deletion due to unpaid fines (BR-22)
                        await auditLogRepository.AddAsync(new AuditLog
                        {
                            UserId = request.CurrentUserId,
                            ActionType = AuditActionType.Delete,
                            EntityType = "User",
                            EntityId = user.Id.ToString(),
                            EntityName = user.FullName,
                            Details = $"Failed to delete user {user.Email} - has unpaid fines",
                            Module = "UserManagement",
                            IsSuccess = false,
                            ErrorMessage = "User has unpaid fines"
                        });
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        return Result.Failure("Cannot delete user with unpaid fines. Please clear all fines first.");
                    }
                    
                    // Delete member
                    memberRepository.Delete(member);
                }
            }
            
            if (user.Librarian != null)
            {
                // UC009 Exception 9.0.E6: Librarian Has Active Responsibilities
                if (user.Role == UserRole.Admin)
                {
                    // Check if this is the only admin
                    var adminCount = await userRepository.CountAsync(u => u.Role == UserRole.Admin);
                    if (adminCount <= 1)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        
                        // Log failed deletion - last admin (BR-22)
                        await auditLogRepository.AddAsync(new AuditLog
                        {
                            UserId = request.CurrentUserId,
                            ActionType = AuditActionType.Delete,
                            EntityType = "User",
                            EntityId = user.Id.ToString(),
                            EntityName = user.FullName,
                            Details = $"Failed to delete user {user.Email} - last admin account",
                            Module = "UserManagement",
                            IsSuccess = false,
                            ErrorMessage = "Cannot delete last administrator account"
                        });
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        return Result.Failure("Cannot delete the last administrator account.");
                    }
                }
                
                // Delete librarian record
                librarianRepository.Delete(user.Librarian);
            }
            
            // Log successful user deletion (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.Delete,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"User {user.Email} successfully deleted by user {request.CurrentUserId}",
                Module = "UserManagement",
                IsSuccess = true
            });
            
            // Delete user record
            userRepository.Delete(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            
            // Log exception (BR-22)
            await auditLogRepository.AddAsync(new AuditLog
            {
                UserId = request.CurrentUserId,
                ActionType = AuditActionType.Delete,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                EntityName = user.FullName,
                Details = $"Exception occurred while deleting user {user.Email}",
                Module = "UserManagement",
                IsSuccess = false,
                ErrorMessage = ex.Message
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure($"Failed to delete user: {ex.Message}");
        }
    }
}