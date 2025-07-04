using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Books.Commands;

/// <summary>
/// Command to delete a book from the catalog (UC012 - Delete Book).
/// </summary>
/// <remarks>
/// This implementation follows UC012 specifications:
/// - Validates book exists before deletion (Normal Flow 12.0 step 4)
/// - Checks for active loans and prevents deletion if found (UC012.E1: Active Dependencies Found)
/// - Checks for active reservations and prevents deletion if found (UC012.E2: Active Reservations Found)
/// - Automatically deletes all associated book copies (POST-2)
/// - Records deletion in the audit log (POST-4)
/// - Maintains historical loan records for audit purposes (POST-5)
/// - Supports alternative to deletion by changing status (Alternative Flow 12.1: Book Status Change)
/// 
/// Business Rules Enforced:
/// - BR-06: Book Management Rights (Only Librarian or Admin can delete books)
/// - BR-07: Book Deletion Restriction (Books with active loans or reservations cannot be deleted)
/// - BR-22: Audit Logging Requirement (All key actions logged with timestamps)
/// </remarks>
public record DeleteBookCommand(int Id) : IRequest<Result>;

public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var bookRepository = _unitOfWork.Repository<Book>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            var reservationRepository = _unitOfWork.Repository<Reservation>();
            
            // Get book with copies (PRE-3: Book must exist in the catalog)
            var book = await bookRepository.GetAsync(
                b => b.Id == request.Id,
                b => b.Copies,
                b => b.Categories
            );
            
            if (book == null)
            {
                return Result.Failure($"Book with ID {request.Id} not found."); // UC012.E3: Book Not Found
            }
            
            // Check if book has active loans (PRE-4: Book must not have any active loans)
            if (book.Copies.Any())
            {
                var copyIds = book.Copies.Select(c => c.Id).ToList();
                var hasActiveLoans = await loanRepository.ExistsAsync(
                    l => copyIds.Contains(l.BookCopyId) && 
                        (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
                );
                
                if (hasActiveLoans)
                {
                    return Result.Failure("Cannot delete book with active loans."); // UC012.E1: Active Dependencies Found
                }
            }
            
            // Check if book has active reservations (PRE-5: Book must not have any active reservations)
            var hasActiveReservations = await reservationRepository.ExistsAsync(
                r => r.BookId == request.Id && r.Status == ReservationStatus.Active
            );
            
            if (hasActiveReservations)
            {
                return Result.Failure("Cannot delete book with active reservations."); // UC012.E2: Active Reservations Found
            }
            
            // Record information for audit log before deletion
            var bookInfo = new
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                CopiesCount = book.Copies.Count,
                CategoriesCount = book.Categories.Count
            };
            
            // Delete book and all its copies (POST-1, POST-2, POST-3)
            bookRepository.Delete(book); // CASCADE DELETE will handle copies and category associations
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Record audit log for deletion (POST-4)
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Book",
                EntityId = bookInfo.Id.ToString(),
                EntityName = bookInfo.Title,
                ActionType = AuditActionType.Delete,
                Details = $"Book '{bookInfo.Title}' with ISBN '{bookInfo.ISBN}' was deleted with {bookInfo.CopiesCount} copies.",
                BeforeState = System.Text.Json.JsonSerializer.Serialize(bookInfo),
                IsSuccess = true
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _unitOfWork.CommitTransactionAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"An error occurred while deleting the book: {ex.Message}"); // UC012.E5: Data Constraint Violation
        }
    }
}