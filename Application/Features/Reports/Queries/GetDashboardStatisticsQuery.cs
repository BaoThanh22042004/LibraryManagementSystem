using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq;

namespace Application.Features.Reports.Queries;

/// <summary>
/// Query to retrieve comprehensive dashboard statistics
/// </summary>
public record GetDashboardStatisticsQuery(DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<Result<DashboardDto>>;

public class GetDashboardStatisticsQueryHandler : IRequestHandler<GetDashboardStatisticsQuery, Result<DashboardDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardStatisticsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DashboardDto>> Handle(GetDashboardStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Create dashboard DTO to be populated
        var dashboardDto = new DashboardDto
        {
            GeneratedAt = DateTime.UtcNow
        };

        // Set date range for statistics
        var endDate = request.EndDate ?? DateTime.UtcNow;
        var last7Days = endDate.AddDays(-7);
        var last30Days = endDate.AddDays(-30);

        // Repositories needed for statistics
        var userRepository = _unitOfWork.Repository<User>();
        var memberRepository = _unitOfWork.Repository<Member>();
        var bookRepository = _unitOfWork.Repository<Book>();
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        var categoryRepository = _unitOfWork.Repository<Category>();
        var loanRepository = _unitOfWork.Repository<Loan>();
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        var fineRepository = _unitOfWork.Repository<Fine>();
        var notificationRepository = _unitOfWork.Repository<Notification>();

        // Get user and membership statistics
        await GetMembershipStatistics(dashboardDto, memberRepository, last30Days);
        
        // Get collection statistics
        await GetCollectionStatistics(dashboardDto, bookRepository, bookCopyRepository, categoryRepository);
        
        // Get loan statistics
        await GetLoanStatistics(dashboardDto, loanRepository, reservationRepository, last7Days);
        
        // Get financial statistics
        await GetFinancialStatistics(dashboardDto, fineRepository, last30Days);
        
        // Get system activity statistics
        await GetActivityStatistics(dashboardDto, notificationRepository, userRepository, last7Days);

        return Result.Success(dashboardDto);
    }

    private static async Task GetMembershipStatistics(
        DashboardDto dashboardDto, 
        IRepository<Member> memberRepository,
        DateTime last30Days)
    {
        var membershipStats = dashboardDto.MembershipStats;

        // Total registered members
        membershipStats.TotalMembers = await memberRepository.CountAsync();

        // Active members
        membershipStats.ActiveMembers = await memberRepository.CountAsync(m => m.MembershipStatus == MembershipStatus.Active);

        // New members in last 30 days
        membershipStats.NewMembersLast30Days = await memberRepository.CountAsync(m => m.CreatedAt >= last30Days);

        // Members by status breakdown
        var memberStatusCounts = await memberRepository.ListAsync(
            orderBy: q => q.OrderBy(m => m.Id)
        );

        foreach (var status in Enum.GetValues<MembershipStatus>())
        {
            membershipStats.MembersByStatus[status] = memberStatusCounts.Count(m => m.MembershipStatus == status);
        }
    }

    private static async Task GetCollectionStatistics(
        DashboardDto dashboardDto,
        IRepository<Book> bookRepository,
        IRepository<BookCopy> bookCopyRepository,
        IRepository<Category> categoryRepository)
    {
        var collectionStats = dashboardDto.CollectionStats;

        // Total books
        collectionStats.TotalBooks = await bookRepository.CountAsync();

        // Total copies
        collectionStats.TotalCopies = await bookCopyRepository.CountAsync();

        // Available copies
        collectionStats.AvailableCopies = await bookCopyRepository.CountAsync(bc => bc.Status == CopyStatus.Available);

		// Borrowed copies
		collectionStats.BorrowedCopies = await bookCopyRepository.CountAsync(bc => bc.Status == CopyStatus.Borrowed);

        // Total categories
        collectionStats.TotalCategories = await categoryRepository.CountAsync();

        // Copies by status breakdown
        var copyStatusCounts = await bookCopyRepository.ListAsync(
            orderBy: q => q.OrderBy(bc => bc.Id)
        );

        foreach (var status in Enum.GetValues<CopyStatus>())
        {
            collectionStats.CopiesByStatus[status] = copyStatusCounts.Count(bc => bc.Status == status);
        }
    }

    private static async Task GetLoanStatistics(
        DashboardDto dashboardDto,
        IRepository<Loan> loanRepository,
        IRepository<Reservation> reservationRepository,
        DateTime last7Days)
    {
        var loanStats = dashboardDto.LoanStats;

        // Active loans
        loanStats.ActiveLoans = await loanRepository.CountAsync(l => l.Status == LoanStatus.Active);

        // Overdue loans
        loanStats.OverdueLoans = await loanRepository.CountAsync(l => l.Status == LoanStatus.Active && l.DueDate < DateTime.UtcNow);

        // Loans issued in last 7 days
        loanStats.LoansIssuedLast7Days = await loanRepository.CountAsync(l => l.LoanDate >= last7Days);

        // Returns in last 7 days
        loanStats.ReturnsLast7Days = await loanRepository.CountAsync(l => 
            l.Status == LoanStatus.Returned && 
            l.ReturnDate.HasValue && 
            l.ReturnDate.Value >= last7Days);

        // Active reservations
        loanStats.ActiveReservations = await reservationRepository.CountAsync(r => r.Status == ReservationStatus.Active);

        // Fulfilled reservations waiting for pickup
        loanStats.FulfilledReservations = await reservationRepository.CountAsync(r => r.Status == ReservationStatus.Fulfilled);

        // Loans by status breakdown
        var loanStatusCounts = await loanRepository.ListAsync(
            orderBy: q => q.OrderBy(l => l.Id)
        );

        foreach (var status in Enum.GetValues<LoanStatus>())
        {
            loanStats.LoansByStatus[status] = loanStatusCounts.Count(l => l.Status == status);
        }
    }

    private static async Task GetFinancialStatistics(
        DashboardDto dashboardDto,
        IRepository<Fine> fineRepository,
        DateTime last30Days)
    {
        var financialStats = dashboardDto.FinancialStats;

        // Total pending fines
        var pendingFines = await fineRepository.ListAsync(f => f.Status == FineStatus.Pending);
        financialStats.TotalPendingFines = pendingFines.Sum(f => f.Amount);

        // Fines collected in last 30 days
        var paidFines = await fineRepository.ListAsync(f => 
            f.Status == FineStatus.Paid && 
            f.LastModifiedAt.HasValue && 
            f.LastModifiedAt.Value >= last30Days);
        financialStats.FinesCollectedLast30Days = paidFines.Sum(f => f.Amount);

        // Fines waived in last 30 days
        var waivedFines = await fineRepository.ListAsync(f => 
            f.Status == FineStatus.Waived && 
            f.LastModifiedAt.HasValue && 
            f.LastModifiedAt.Value >= last30Days);
        financialStats.FinesWaivedLast30Days = waivedFines.Sum(f => f.Amount);

        // Members with outstanding fines
        var membersWithFines = await fineRepository.ListAsync(f => f.Status == FineStatus.Pending);
        financialStats.MembersWithFines = membersWithFines.Select(f => f.MemberId).Distinct().Count();

        // Average fine amount
        var allFines = await fineRepository.ListAsync();
        if (allFines.Any())
        {
            financialStats.AverageFineAmount = allFines.Average(f => f.Amount);
        }

        // Fines by status breakdown
        var finesByStatus = await fineRepository.ListAsync(
            orderBy: q => q.OrderBy(f => f.Id)
        );

        foreach (var status in Enum.GetValues<FineStatus>())
        {
            financialStats.FinesByStatus[status] = finesByStatus.Where(f => f.Status == status).Sum(f => f.Amount);
        }
    }

    private static async Task GetActivityStatistics(
        DashboardDto dashboardDto,
        IRepository<Notification> notificationRepository,
        IRepository<User> userRepository,
        DateTime last7Days)
    {
        var activityStats = dashboardDto.ActivityStats;

        // Notifications sent in last 7 days
        activityStats.NotificationsSentLast7Days = await notificationRepository.CountAsync(n => 
            n.Status == NotificationStatus.Sent && 
            n.SentAt.HasValue && 
            n.SentAt.Value >= last7Days);

        // Pending notifications
        activityStats.PendingNotifications = await notificationRepository.CountAsync(n => n.Status == NotificationStatus.Pending);

        // Users by role
        var usersByRole = await userRepository.ListAsync(
            orderBy: q => q.OrderBy(u => u.Id)
        );

        foreach (var role in Enum.GetValues<UserRole>())
        {
            activityStats.UsersByRole[role] = usersByRole.Count(u => u.Role == role);
        }

        // Active staff members (Librarians and Admins)
        activityStats.ActiveStaffMembers = await userRepository.CountAsync(u => 
            u.Role == UserRole.Librarian || u.Role == UserRole.Admin);
    }
}