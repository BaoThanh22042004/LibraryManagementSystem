using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
        notification.SentAt = DateTime.UtcNow;
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
        // Only allow Pending -> Sent/Failed, or Failed -> Sent
        if (notification.Status == NotificationStatus.Pending)
        {
            if (dto.Status != NotificationStatus.Sent && dto.Status != NotificationStatus.Failed)
                return Result.Failure<NotificationReadDto>($"Cannot change status from Pending to {dto.Status}.");
        }
        else if (notification.Status == NotificationStatus.Failed)
        {
            if (dto.Status != NotificationStatus.Sent)
                return Result.Failure<NotificationReadDto>($"Can only change status from Failed to Sent.");
        }
        else if (notification.Status == NotificationStatus.Sent)
        {
            return Result.Failure<NotificationReadDto>("Cannot change status of a notification that is already Sent.");
        }
        else if (notification.Status == NotificationStatus.Read)
        {
            return Result.Failure<NotificationReadDto>("Cannot change status of a notification that is already Read.");
        }
        // Apply status change
        notification.Status = dto.Status;
        if (dto.Status == NotificationStatus.Sent && notification.SentAt == null)
            notification.SentAt = DateTime.UtcNow;
        repo.Update(notification);
        await _unitOfWork.SaveChangesAsync();
        var readDto = _mapper.Map<NotificationReadDto>(notification);
        return Result.Success(readDto);
    }

    public async Task<Result<(int SuccessCount, List<(int Id, string Reason)> Failures)>> MarkAsReadAsync(NotificationMarkAsReadDto dto, int memberId)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notifications = await repo.ListAsync(n => dto.NotificationIds.Contains(n.Id));
        var failures = new List<(int Id, string Reason)>();
        int successCount = 0;
        foreach (var id in dto.NotificationIds)
        {
            var n = notifications.FirstOrDefault(x => x.Id == id);
            if (n == null)
            {
                failures.Add((id, "Not found"));
                continue;
            }
            if (n.UserId != memberId)
            {
                failures.Add((id, "Not owned by user"));
                continue;
            }
            if (n.Status == NotificationStatus.Read)
            {
                failures.Add((id, "Already read"));
                continue;
            }
            n.Status = NotificationStatus.Read;
            n.ReadAt = DateTime.UtcNow;
            repo.Update(n);
            successCount++;
        }
        if (successCount > 0) await _unitOfWork.SaveChangesAsync();
        if (successCount == 0)
            return Result.Failure<(int, List<(int, string)>)>("No unread notifications found to mark as read.", (0, failures ?? new List<(int, string)>()));
        return Result.Success((successCount, failures));
    }

    public async Task<Result<List<NotificationListDto>>> GetNotificationsAsync(int memberId, bool unreadOnly = false)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notifications = await repo.ListAsync(
            n => (n.UserId == memberId || n.UserId == null) && (!unreadOnly || n.Status != NotificationStatus.Read),
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
        if (notification.UserId == null)
        {
            // System-wide notification: allow any authenticated user
            var dto = _mapper.Map<NotificationReadDto>(notification);
            return Result.Success(dto);
        }
        if (!isStaff && notification.UserId != requesterId)
            return Result.Failure<NotificationReadDto>("Access denied.");
        var dto2 = _mapper.Map<NotificationReadDto>(notification);
        return Result.Success(dto2);
    }

    public async Task<Result<int>> GetUnreadCountAsync(int memberId)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var count = await repo.CountAsync(n => n.UserId == memberId && n.Status != NotificationStatus.Read);
        return Result.Success(count);
    }

    public async Task<Result<(int SuccessCount, List<string> Errors)>> SendOverdueNotificationsAsync()
    {
        var loanRepo = _unitOfWork.Repository<Loan>();
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var now = DateTime.UtcNow;
        var overdueLoans = await loanRepo.ListAsync(
            l => l.Status == Domain.Enums.LoanStatus.Overdue && l.DueDate < now,
            null,
            false,
            l => l.Member.User,
            l => l.BookCopy.Book);
        int sentCount = 0;
        var errors = new List<string>();
        foreach (var loan in overdueLoans)
        {
            try
            {
                bool alreadyNotified = await notificationRepo.ExistsAsync(n =>
                    n.UserId == loan.Member.UserId &&
                    n.Type == NotificationType.OverdueNotice &&
                    n.Subject.Contains(loan.BookCopy.Book.Title) &&
                    n.Status != NotificationStatus.Failed &&
                    n.CreatedAt > loan.DueDate
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
            catch (Exception ex)
            {
                errors.Add($"LoanId {loan.Id}: {ex.Message}");
            }
        }
        if (sentCount > 0) await _unitOfWork.SaveChangesAsync();
        return Result.Success((sentCount, errors));
    }

    public async Task<Result<(int SuccessCount, List<string> Errors)>> SendAvailabilityNotificationsAsync()
    {
        var reservationRepo = _unitOfWork.Repository<Reservation>();
        var bookCopyRepo = _unitOfWork.Repository<BookCopy>();
        var notificationRepo = _unitOfWork.Repository<Notification>();
        var now = DateTime.UtcNow;
        var reservations = await reservationRepo.ListAsync(
            r => r.Status == Domain.Enums.ReservationStatus.Active,
            q => q.OrderBy(r => r.ReservationDate),
            false,
            r => r.Member.User,
            r => r.Book);
        int sentCount = 0;
        var errors = new List<string>();
        var reservationsByBook = reservations.GroupBy(r => r.BookId);
        foreach (var group in reservationsByBook)
        {
            try
            {
                var availableCopy = await bookCopyRepo.GetAsync(bc => bc.BookId == group.Key && bc.Status == Domain.Enums.CopyStatus.Available);
                if (availableCopy == null) continue;
                var reservation = group.OrderBy(r => r.ReservationDate).FirstOrDefault();
                if (reservation == null) continue;
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
            catch (Exception ex)
            {
                errors.Add($"ReservationId {group.FirstOrDefault()?.Id}: {ex.Message}");
            }
        }
        if (sentCount > 0) await _unitOfWork.SaveChangesAsync();
        return Result.Success((sentCount, errors));
    }
    public async Task<Result<PagedResult<NotificationListDto>>> GetPagedNotificationsAsync(int userId, bool unreadOnly, int page, int pageSize)
    {
        var query = _unitOfWork.Repository<Notification>()
            .Query()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        var totalRecords = await query.CountAsync();

        var data = await query
        .OrderByDescending(n => n.SentAt ?? DateTime.MinValue) 
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ProjectTo<NotificationListDto>(_mapper.ConfigurationProvider)
        .ToListAsync();

        var pagedResult = new PagedResult<NotificationListDto>
        {
            Items = data,
            Page = page,
            PageSize = pageSize,
            Count = totalRecords
        };

        return Result<PagedResult<NotificationListDto>>.Success(pagedResult); 
    }
    public async Task<Result<PagedResult<NotificationListDto>>> GetPagedAdminNotificationsAsync(string? search, int page, int pageSize, string sortBy, string sortOrder)
    {
        var query = _unitOfWork.Repository<Notification>().Query();

        // Tìm kiếm theo Subject, Message, Email
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim().ToLower();
            query = query.Where(n =>
                n.Subject.ToLower().Contains(keyword) ||
                n.Message.ToLower().Contains(keyword) ||
                (n.User != null && n.User.Email.ToLower().Contains(keyword)));
        }

        var totalRecords = await query.CountAsync();

        // Sắp xếp
        query = sortBy switch
        {
            "CreatedAt" => sortOrder == "asc" ? query.OrderBy(n => n.CreatedAt) : query.OrderByDescending(n => n.CreatedAt),
            _ => sortOrder == "asc" ? query.OrderBy(n => n.SentAt ?? DateTime.MinValue) : query.OrderByDescending(n => n.SentAt ?? DateTime.MinValue)
        };

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<NotificationListDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        var result = new PagedResult<NotificationListDto>
        {
            Items = data,
            Page = page,
            PageSize = pageSize,
            Count = totalRecords
        };

        return Result.Success(result);
    }

}
