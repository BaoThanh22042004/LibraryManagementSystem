using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Notifications.Commands;

/// <summary>
/// Command to send notifications to users with overdue loans.
/// </summary>
public record SendOverdueNotificationsCommand : IRequest<Result>;

public class SendOverdueNotificationsCommandHandler : IRequestHandler<SendOverdueNotificationsCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public SendOverdueNotificationsCommandHandler(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<Result> Handle(SendOverdueNotificationsCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            
            // Get all overdue loans
            var overdueLoans = await loanRepository.ListAsync(
                predicate: l => l.Status == LoanStatus.Active && l.DueDate < DateTime.UtcNow,
                orderBy: q => q.OrderByDescending(l => l.DueDate),
                true,
                l => l.Member.User,
                l => l.BookCopy.Book
            );
            
            if (overdueLoans.Count == 0)
                return Result.Success("No overdue loans found.");
            
            int successCount = 0;
            List<string> errors = new();
            
            // Create a notification for each overdue loan
            foreach (var loan in overdueLoans)
            {
                // Skip if user is null
                if (loan.Member?.User == null)
                {
                    errors.Add($"Loan ID {loan.Id}: User information missing");
                    continue;
                }
                
                var userId = loan.Member.User.Id;
                var bookTitle = loan.BookCopy?.Book?.Title ?? "Unknown Book";
                var daysOverdue = (int)(DateTime.UtcNow - loan.DueDate).TotalDays;
                
                var createNotificationDto = new CreateNotificationDto
                {
                    UserId = userId,
                    Type = NotificationType.OverdueNotice,
                    Subject = $"Overdue Book: {bookTitle}",
                    Message = $"Your loan for '{bookTitle}' is overdue by {daysOverdue} days. Please return the book as soon as possible to avoid additional fines. Current fine: ${daysOverdue * 0.50:F2}"
                };
                
                var result = await _mediator.Send(new CreateNotificationCommand(createNotificationDto), cancellationToken);
                
                if (result.IsSuccess)
                {
                    // Update the notification to 'Sent' status
                    await _mediator.Send(new UpdateNotificationCommand(result.Value, new UpdateNotificationDto { Status = NotificationStatus.Sent }), cancellationToken);
                    successCount++;
                }
                else
                {
                    errors.Add($"Loan ID {loan.Id}: {result.Error}");
                }
            }
            
            await _unitOfWork.CommitTransactionAsync();
            
            string resultMessage = $"Successfully sent {successCount} overdue notifications.";
            if (errors.Count > 0)
            {
                resultMessage += $" Encountered {errors.Count} errors: {string.Join("; ", errors)}";
            }
            
            return Result.Success(resultMessage);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to send overdue notifications: {ex.Message}");
        }
    }
}