using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Represents comprehensive dashboard statistics for the library system
/// </summary>
public class DashboardDto
{
    /// <summary>
    /// Current membership statistics
    /// </summary>
    public MembershipStatsDto MembershipStats { get; set; } = new();
    
    /// <summary>
    /// Collection usage statistics
    /// </summary>
    public CollectionStatsDto CollectionStats { get; set; } = new();
    
    /// <summary>
    /// Loan activity statistics
    /// </summary>
    public LoanStatsDto LoanStats { get; set; } = new();
    
    /// <summary>
    /// Financial metrics
    /// </summary>
    public FinancialStatsDto FinancialStats { get; set; } = new();
    
    /// <summary>
    /// System activity metrics
    /// </summary>
    public ActivityStatsDto ActivityStats { get; set; } = new();
    
    /// <summary>
    /// Dashboard generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Statistics about library membership
/// </summary>
public class MembershipStatsDto
{
    /// <summary>
    /// Total number of registered members
    /// </summary>
    public int TotalMembers { get; set; }
    
    /// <summary>
    /// Number of active members
    /// </summary>
    public int ActiveMembers { get; set; }
    
    /// <summary>
    /// Number of new members in the last 30 days
    /// </summary>
    public int NewMembersLast30Days { get; set; }
    
    /// <summary>
    /// Members by status breakdown
    /// </summary>
    public Dictionary<MembershipStatus, int> MembersByStatus { get; set; } = new();
}

/// <summary>
/// Statistics about the library collection
/// </summary>
public class CollectionStatsDto
{
    /// <summary>
    /// Total number of unique book titles
    /// </summary>
    public int TotalBooks { get; set; }
    
    /// <summary>
    /// Total number of physical copies across all books
    /// </summary>
    public int TotalCopies { get; set; }
    
    /// <summary>
    /// Number of available copies ready for checkout
    /// </summary>
    public int AvailableCopies { get; set; }
    
    /// <summary>
    /// Number of copies currently on loan
    /// </summary>
    public int BorrowedCopies { get; set; }
    
    /// <summary>
    /// Number of categories in the system
    /// </summary>
    public int TotalCategories { get; set; }
    
    /// <summary>
    /// Copies by status breakdown
    /// </summary>
    public Dictionary<CopyStatus, int> CopiesByStatus { get; set; } = new();
}

/// <summary>
/// Statistics about loan activity
/// </summary>
public class LoanStatsDto
{
    /// <summary>
    /// Total number of active loans
    /// </summary>
    public int ActiveLoans { get; set; }
    
    /// <summary>
    /// Number of overdue loans
    /// </summary>
    public int OverdueLoans { get; set; }
    
    /// <summary>
    /// Number of loans issued in the last 7 days
    /// </summary>
    public int LoansIssuedLast7Days { get; set; }
    
    /// <summary>
    /// Number of returns processed in the last 7 days
    /// </summary>
    public int ReturnsLast7Days { get; set; }
    
    /// <summary>
    /// Total number of active reservations
    /// </summary>
    public int ActiveReservations { get; set; }
    
    /// <summary>
    /// Number of fulfilled reservations waiting for pickup
    /// </summary>
    public int FulfilledReservations { get; set; }
    
    /// <summary>
    /// Loans by status breakdown
    /// </summary>
    public Dictionary<LoanStatus, int> LoansByStatus { get; set; } = new();
}

/// <summary>
/// Financial statistics
/// </summary>
public class FinancialStatsDto
{
    /// <summary>
    /// Total amount of pending fines
    /// </summary>
    public decimal TotalPendingFines { get; set; }
    
    /// <summary>
    /// Total amount of fines collected in the last 30 days
    /// </summary>
    public decimal FinesCollectedLast30Days { get; set; }
    
    /// <summary>
    /// Total amount of fines waived in the last 30 days
    /// </summary>
    public decimal FinesWaivedLast30Days { get; set; }
    
    /// <summary>
    /// Number of members with outstanding fines
    /// </summary>
    public int MembersWithFines { get; set; }
    
    /// <summary>
    /// Average fine amount per overdue loan
    /// </summary>
    public decimal AverageFineAmount { get; set; }
    
    /// <summary>
    /// Fines by status breakdown
    /// </summary>
    public Dictionary<FineStatus, decimal> FinesByStatus { get; set; } = new();
}

/// <summary>
/// System activity statistics
/// </summary>
public class ActivityStatsDto
{
    /// <summary>
    /// Number of notifications sent in the last 7 days
    /// </summary>
    public int NotificationsSentLast7Days { get; set; }
    
    /// <summary>
    /// Number of pending notifications to be sent
    /// </summary>
    public int PendingNotifications { get; set; }
    
    /// <summary>
    /// Count of system users by role
    /// </summary>
    public Dictionary<UserRole, int> UsersByRole { get; set; } = new();
    
    /// <summary>
    /// Number of active staff members (Librarians and Admins)
    /// </summary>
    public int ActiveStaffMembers { get; set; }
}