using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<NotificationReadDto>> CreateNotificationAsync(NotificationCreateDto dto)
    {
        // Validate member if UserId specified
        if (dto.UserId.HasValue)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var userExists = await userRepo.ExistsAsync(u => u.Id == dto.UserId.Value);
            if (!userExists)
                return Result.Failure<NotificationReadDto>("Member not found.");
        }
        var notification = _mapper.Map<Notification>(dto);
        notification.Status = NotificationStatus.Pending;
        notification.CreatedAt = DateTime.UtcNow;
        var repo = _unitOfWork.Repository<Notification>();
        await repo.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
        var readDto = _mapper.Map<NotificationReadDto>(notification);
        return Result.Success(readDto);
    }

    public async Task<Result<List<NotificationReadDto>>> CreateNotificationsBulkAsync(NotificationBatchCreateDto dto)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var users = await userRepo.ListAsync(u => dto.UserIds.Contains(u.Id));
        if (users.Count != dto.UserIds.Count)
            return Result.Failure<List<NotificationReadDto>>("One or more members not found.");
        var notifications = dto.UserIds.Select(userId => new Notification
        {
            UserId = userId,
            Type = dto.Type,
            Subject = dto.Subject,
            Message = dto.Message,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        var repo = _unitOfWork.Repository<Notification>();
        await repo.AddRangeAsync(notifications);
        await _unitOfWork.SaveChangesAsync();
        var readDtos = notifications.Select(_mapper.Map<NotificationReadDto>).ToList();
        return Result.Success(readDtos);
    }

    public async Task<Result<NotificationReadDto>> UpdateNotificationStatusAsync(NotificationUpdateStatusDto dto)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notification = await repo.GetAsync(n => n.Id == dto.NotificationId);
        if (notification == null)
            return Result.Failure<NotificationReadDto>("Notification not found.");
        // Validate status transition
        if (!Enum.IsDefined(typeof(NotificationStatus), dto.Status))
            return Result.Failure<NotificationReadDto>("Invalid status value.");
        if (notification.Status == NotificationStatus.Sent && dto.Status == NotificationStatus.Sent)
        {
            // Already sent, preserve SentAt
        }
        else
        {
            notification.Status = dto.Status;
            if (dto.Status == NotificationStatus.Sent && notification.SentAt == null)
                notification.SentAt = DateTime.UtcNow;
        }
        repo.Update(notification);
        await _unitOfWork.SaveChangesAsync();
        var readDto = _mapper.Map<NotificationReadDto>(notification);
        return Result.Success(readDto);
    }

    public async Task<Result<int>> MarkAsReadAsync(NotificationMarkAsReadDto dto, int memberId)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notifications = await repo.ListAsync(n => dto.NotificationIds.Contains(n.Id) && n.UserId == memberId && n.Status != NotificationStatus.Read);
        if (notifications.Count == 0)
            return Result.Failure<int>("No unread notifications found to mark as read.");
        foreach (var n in notifications)
        {
            n.Status = NotificationStatus.Read;
            n.ReadAt = DateTime.UtcNow;
            repo.Update(n);
        }
        await _unitOfWork.SaveChangesAsync();
        return Result.Success(notifications.Count);
    }

    public async Task<Result<List<NotificationListDto>>> GetNotificationsAsync(int memberId, bool unreadOnly = false)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notifications = await repo.ListAsync(
            n => n.UserId == memberId && (!unreadOnly || n.Status != NotificationStatus.Read),
            q => q.OrderByDescending(n => n.SentAt ?? n.CreatedAt));
        var dtos = notifications.Select(_mapper.Map<NotificationListDto>).ToList();
        return Result.Success(dtos);
    }

    public async Task<Result<NotificationReadDto>> GetNotificationDetailAsync(int notificationId, int requesterId, bool isStaff = false)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notification = await repo.GetAsync(n => n.Id == notificationId, n => n.User!);
        if (notification == null)
            return Result.Failure<NotificationReadDto>("Notification not found.");
        if (!isStaff && notification.UserId != requesterId)
            return Result.Failure<NotificationReadDto>("Access denied.");
        var dto = _mapper.Map<NotificationReadDto>(notification);
        return Result.Success(dto);
    }

    public async Task<Result<int>> GetUnreadCountAsync(int memberId)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var count = await repo.CountAsync(n => n.UserId == memberId && n.Status != NotificationStatus.Read);
        return Result.Success(count);
    }

    public async Task<Result<int>> SendOverdueNotificationsAsync()
    {
        var loanRepo = _unitOfWork.Repository<Loan>();
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var now = DateTime.UtcNow;
        // Find all overdue loans
        var overdueLoans = await loanRepo.ListAsync(
            l => l.Status == Domain.Enums.LoanStatus.Overdue && l.DueDate < now,
            null,
            false,
            l => l.Member.User,
            l => l.BookCopy.Book);
        int sentCount = 0;
        foreach (var loan in overdueLoans)
        {
            // Check if notification already sent for this loan/member
            bool alreadyNotified = await notificationRepo.ExistsAsync(n =>
                n.UserId == loan.Member.UserId &&
                n.Type == NotificationType.OverdueNotice &&
                n.Subject.Contains(loan.BookCopy.Book.Title) &&
                n.Status != NotificationStatus.Failed &&
                n.CreatedAt > loan.DueDate // Only after overdue
            );
            if (alreadyNotified) continue;
            var daysOverdue = (now.Date - loan.DueDate.Date).Days;
            var subject = $"Overdue Notice: {loan.BookCopy.Book.Title}";
            var message = $"Your loan for '{loan.BookCopy.Book.Title}' is overdue by {daysOverdue} day(s). Please return it as soon as possible.";
            var notification = new Notification
            {
                UserId = loan.Member.UserId,
                Type = NotificationType.OverdueNotice,
                Subject = subject,
                Message = message,
                Status = NotificationStatus.Sent,
                SentAt = now,
                CreatedAt = now
            };
            await notificationRepo.AddAsync(notification);
            sentCount++;
        }
        if (sentCount > 0) await _unitOfWork.SaveChangesAsync();
        return Result.Success(sentCount);
    }

    public async Task<Result<int>> SendAvailabilityNotificationsAsync()
    {
        var reservationRepo = _unitOfWork.Repository<Reservation>();
        var bookCopyRepo = _unitOfWork.Repository<BookCopy>();
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var now = DateTime.UtcNow;
        // Find all active reservations ordered by reservation date
        var reservations = await reservationRepo.ListAsync(
            r => r.Status == Domain.Enums.ReservationStatus.Active,
            q => q.OrderBy(r => r.ReservationDate),
            false,
            r => r.Member.User,
            r => r.Book);
        int sentCount = 0;
        // Group by book
        var reservationsByBook = reservations.GroupBy(r => r.BookId);
        foreach (var group in reservationsByBook)
        {
            // Check if any copy of the book is available
            var availableCopy = await bookCopyRepo.GetAsync(bc => bc.BookId == group.Key && bc.Status == Domain.Enums.CopyStatus.Available);
            if (availableCopy == null) continue;
            // Notify the first reservation in the queue
            var reservation = group.OrderBy(r => r.ReservationDate).FirstOrDefault();
            if (reservation == null) continue;
            // Check if notification already sent for this reservation/member
            bool alreadyNotified = await notificationRepo.ExistsAsync(n =>
                n.UserId == reservation.Member.UserId &&
                n.Type == NotificationType.ReservationAvailable &&
                n.Subject.Contains(reservation.Book.Title) &&
                n.Status != NotificationStatus.Failed &&
                n.CreatedAt > (availableCopy.LastModifiedAt ?? availableCopy.CreatedAt)
            );
            if (alreadyNotified) continue;
            var pickupDeadline = now.AddDays(3);
            var subject = $"Reservation Available: {reservation.Book.Title}";
            var message = $"Your reserved book '{reservation.Book.Title}' is now available. Please pick it up by {pickupDeadline:yyyy-MM-dd}.";
            var notification = new Notification
            {
                UserId = reservation.Member.UserId,
                Type = NotificationType.ReservationAvailable,
                Subject = subject,
                Message = message,
                Status = NotificationStatus.Sent,
                SentAt = now,
                CreatedAt = now
            };
            await notificationRepo.AddAsync(notification);
            sentCount++;
        }
        if (sentCount > 0) await _unitOfWork.SaveChangesAsync();
        return Result.Success(sentCount);
    }
}
