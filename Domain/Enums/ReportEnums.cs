namespace Domain.Enums;

/// <summary>
/// Types of reports that can be generated in the system
/// </summary>
public enum ReportType
{
    /// <summary>
    /// Comprehensive dashboard statistics
    /// </summary>
    Dashboard = 1,
    
    /// <summary>
    /// Report of overdue loans
    /// </summary>
    OverdueLoans = 2,
    
    /// <summary>
    /// Report of all fines
    /// </summary>
    Fines = 3,
    
    /// <summary>
    /// Report of outstanding fines for a specific member
    /// </summary>
    OutstandingFines = 4,
    
    /// <summary>
    /// Report of member activity and statistics
    /// </summary>
    MemberActivity = 5,
    
    /// <summary>
    /// Report of collection usage statistics
    /// </summary>
    CollectionUsage = 6,
    
    /// <summary>
    /// Report of library transactions (loans, returns)
    /// </summary>
    Transactions = 7,
    
    /// <summary>
    /// Report of system activity and audit logs
    /// </summary>
    SystemActivity = 8
}

/// <summary>
/// Output formats for generated reports
/// </summary>
public enum ReportFormat
{
    /// <summary>
    /// PDF document format
    /// </summary>
    PDF = 1,
    
    /// <summary>
    /// Excel spreadsheet format
    /// </summary>
    Excel = 2,
    
    /// <summary>
    /// CSV (Comma Separated Values) format
    /// </summary>
    CSV = 3,
    
    /// <summary>
    /// HTML document format
    /// </summary>
    HTML = 4,
    
    /// <summary>
    /// JSON data format
    /// </summary>
    JSON = 5
}