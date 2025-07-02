using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Enums;
using MediatR;
using System.Text;
using System.Text.Json;

namespace Application.Features.Reports.Commands;

/// <summary>
/// Command to generate a report in a specified format
/// </summary>
public record GenerateReportCommand(GenerateReportDto ReportParameters) : IRequest<Result<ReportResultDto>>;

public class GenerateReportCommandHandler : IRequestHandler<GenerateReportCommand, Result<ReportResultDto>>
{
    private readonly IMediator _mediator;
    private readonly IReportService _reportService;
    private readonly IFileExportService _fileExportService;
    
    public GenerateReportCommandHandler(
        IMediator mediator, 
        IReportService reportService,
        IFileExportService fileExportService)
    {
        _mediator = mediator;
        _reportService = reportService;
        _fileExportService = fileExportService;
    }
    
    public async Task<Result<ReportResultDto>> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        var parameters = request.ReportParameters;
        var reportResult = new ReportResultDto
        {
            ReportType = parameters.ReportType,
            Format = parameters.Format,
            GeneratedAt = DateTime.UtcNow,
            IsSuccess = false
        };
        
        try
        {
            // First, gather the appropriate data based on report type
            object reportData = parameters.ReportType switch
            {
                ReportType.Dashboard => await GetDashboardData(parameters),
                ReportType.OverdueLoans => await GetOverdueLoansData(),
                ReportType.Fines => await GetFinesData(parameters.FineStatusFilter),
                ReportType.OutstandingFines => await GetOutstandingFinesData(parameters.MemberId),
                _ => throw new ArgumentException($"Unsupported report type: {parameters.ReportType}")
            };
            
            // Generate a file name based on report type and timestamp
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string reportTypeName = parameters.ReportType.ToString();
            
            // Generate appropriate content based on format
            switch (parameters.Format)
            {
                case ReportFormat.PDF:
                    reportResult = await GeneratePdfReport(reportData, reportTypeName, timestamp);
                    break;
                
                case ReportFormat.Excel:
                    reportResult = await GenerateExcelReport(reportData, reportTypeName, timestamp);
                    break;
                
                case ReportFormat.CSV:
                    reportResult = await GenerateCsvReport(reportData, reportTypeName, timestamp);
                    break;
                
                case ReportFormat.HTML:
                    reportResult = await GenerateHtmlReport(reportData, reportTypeName, timestamp);
                    break;
                
                case ReportFormat.JSON:
                    reportResult = GenerateJsonReport(reportData, reportTypeName, timestamp);
                    break;
                
                default:
                    return Result.Failure<ReportResultDto>($"Unsupported report format: {parameters.Format}");
            }
            
            return Result.Success(reportResult);
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportResultDto>($"Error generating report: {ex.Message}");
        }
    }
    
    #region Data Retrieval Methods
    
    private async Task<DashboardDto> GetDashboardData(GenerateReportDto parameters)
    {
        if (parameters.StartDate.HasValue && parameters.EndDate.HasValue)
        {
            return await _reportService.GetDashboardStatisticsAsync(
                parameters.StartDate.Value, 
                parameters.EndDate.Value);
        }
        else
        {
            return await _reportService.GetDashboardStatisticsAsync();
        }
    }
    
    private async Task<OverdueReportDto> GetOverdueLoansData()
    {
        return await _reportService.GetOverdueReportAsync();
    }
    
    private async Task<FineReportDto> GetFinesData(FineStatus[]? statusFilter)
    {
        return await _reportService.GetFinesReportAsync(statusFilter);
    }
    
    private async Task<OutstandingFineDto> GetOutstandingFinesData(int? memberId)
    {
        if (!memberId.HasValue)
        {
            throw new ArgumentException("Member ID is required for outstanding fines report");
        }
        
        return await _reportService.GetOutstandingFinesAsync(memberId.Value);
    }
    
    #endregion
    
    #region Report Generation Methods
    
    private async Task<ReportResultDto> GeneratePdfReport(object data, string reportName, string timestamp)
    {
        // Convert the data to HTML first, then to PDF
        string html = await GenerateHtmlContent(data, reportName);
        
        // Generate PDF from HTML using file export service
        string fileName = $"{reportName}_{timestamp}.pdf";
        string filePath = await _fileExportService.ExportToPdfAsync(html, fileName);
        
        long fileSize = new FileInfo(filePath).Length;
        
        return new ReportResultDto
        {
            ReportType = GetReportTypeFromName(reportName),
            Format = ReportFormat.PDF,
            Content = filePath,
            Filename = fileName,
            ContentType = "application/pdf",
            Size = fileSize,
            IsSuccess = true,
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private async Task<ReportResultDto> GenerateExcelReport(object data, string reportName, string timestamp)
    {
        // Generate Excel file using file export service
        string fileName = $"{reportName}_{timestamp}.xlsx";
        string filePath = await _fileExportService.ExportToExcelAsync(data, fileName);
        
        long fileSize = new FileInfo(filePath).Length;
        
        return new ReportResultDto
        {
            ReportType = GetReportTypeFromName(reportName),
            Format = ReportFormat.Excel,
            Content = filePath,
            Filename = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Size = fileSize,
            IsSuccess = true,
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private async Task<ReportResultDto> GenerateCsvReport(object data, string reportName, string timestamp)
    {
        // Generate CSV file using file export service
        string fileName = $"{reportName}_{timestamp}.csv";
        string filePath = await _fileExportService.ExportToCsvAsync(data, fileName);
        
        long fileSize = new FileInfo(filePath).Length;
        
        return new ReportResultDto
        {
            ReportType = GetReportTypeFromName(reportName),
            Format = ReportFormat.CSV,
            Content = filePath,
            Filename = fileName,
            ContentType = "text/csv",
            Size = fileSize,
            IsSuccess = true,
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private async Task<ReportResultDto> GenerateHtmlReport(object data, string reportName, string timestamp)
    {
        // Generate HTML content
        string html = await GenerateHtmlContent(data, reportName);
        
        // Save HTML to file
        string fileName = $"{reportName}_{timestamp}.html";
        string filePath = Path.Combine(Path.GetTempPath(), fileName);
        
        await File.WriteAllTextAsync(filePath, html);
        long fileSize = new FileInfo(filePath).Length;
        
        return new ReportResultDto
        {
            ReportType = GetReportTypeFromName(reportName),
            Format = ReportFormat.HTML,
            Content = filePath,
            Filename = fileName,
            ContentType = "text/html",
            Size = fileSize,
            IsSuccess = true,
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private ReportResultDto GenerateJsonReport(object data, string reportName, string timestamp)
    {
        // Serialize data to JSON
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        // Save JSON to file
        string fileName = $"{reportName}_{timestamp}.json";
        string filePath = Path.Combine(Path.GetTempPath(), fileName);
        
        File.WriteAllText(filePath, json);
        long fileSize = new FileInfo(filePath).Length;
        
        return new ReportResultDto
        {
            ReportType = GetReportTypeFromName(reportName),
            Format = ReportFormat.JSON,
            Content = filePath,
            Filename = fileName,
            ContentType = "application/json",
            Size = fileSize,
            IsSuccess = true,
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private async Task<string> GenerateHtmlContent(object data, string reportName)
    {
        // Simple HTML report template generation
        var sb = new StringBuilder();
        
        // HTML header
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>{reportName} Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("h1 { color: #333366; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine(".summary { margin: 20px 0; padding: 15px; background-color: #f5f5f5; border-radius: 5px; }");
        sb.AppendLine(".timestamp { color: #666; font-size: 0.8em; margin-top: 30px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        
        // Report title
        sb.AppendLine($"<h1>{reportName} Report</h1>");
        
        // Generate content based on report type
        switch (data)
        {
            case DashboardDto dashboard:
                AppendDashboardHtml(sb, dashboard);
                break;
            
            case OverdueReportDto overdueReport:
                AppendOverdueReportHtml(sb, overdueReport);
                break;
            
            case FineReportDto fineReport:
                AppendFineReportHtml(sb, fineReport);
                break;
            
            case OutstandingFineDto outstandingFine:
                AppendOutstandingFineHtml(sb, outstandingFine);
                break;
            
            default:
                sb.AppendLine("<p>Unsupported report type for HTML generation</p>");
                break;
        }
        
        // Timestamp and footer
        sb.AppendLine($"<div class=\"timestamp\">Generated at: {DateTime.UtcNow} UTC</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
    
    #endregion
    
    #region HTML Generation Helpers
    
    private void AppendDashboardHtml(StringBuilder sb, DashboardDto dashboard)
    {
        // Membership statistics
        sb.AppendLine("<h2>Membership Statistics</h2>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>Total Members: <strong>{dashboard.MembershipStats.TotalMembers}</strong></p>");
        sb.AppendLine($"<p>Active Members: <strong>{dashboard.MembershipStats.ActiveMembers}</strong></p>");
        sb.AppendLine($"<p>New Members (Last 30 Days): <strong>{dashboard.MembershipStats.NewMembersLast30Days}</strong></p>");
        sb.AppendLine("</div>");
        
        // Collection statistics
        sb.AppendLine("<h2>Collection Statistics</h2>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>Total Books: <strong>{dashboard.CollectionStats.TotalBooks}</strong></p>");
        sb.AppendLine($"<p>Total Copies: <strong>{dashboard.CollectionStats.TotalCopies}</strong></p>");
        sb.AppendLine($"<p>Available Copies: <strong>{dashboard.CollectionStats.AvailableCopies}</strong></p>");
        sb.AppendLine($"<p>Borrowed Copies: <strong>{dashboard.CollectionStats.BorrowedCopies}</strong></p>");
        sb.AppendLine("</div>");
        
        // Loan statistics
        sb.AppendLine("<h2>Loan Statistics</h2>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>Active Loans: <strong>{dashboard.LoanStats.ActiveLoans}</strong></p>");
        sb.AppendLine($"<p>Overdue Loans: <strong>{dashboard.LoanStats.OverdueLoans}</strong></p>");
        sb.AppendLine($"<p>Loans Issued (Last 7 Days): <strong>{dashboard.LoanStats.LoansIssuedLast7Days}</strong></p>");
        sb.AppendLine($"<p>Returns (Last 7 Days): <strong>{dashboard.LoanStats.ReturnsLast7Days}</strong></p>");
        sb.AppendLine("</div>");
        
        // Financial statistics
        sb.AppendLine("<h2>Financial Statistics</h2>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>Total Pending Fines: <strong>${dashboard.FinancialStats.TotalPendingFines:F2}</strong></p>");
        sb.AppendLine($"<p>Fines Collected (Last 30 Days): <strong>${dashboard.FinancialStats.FinesCollectedLast30Days:F2}</strong></p>");
        sb.AppendLine($"<p>Fines Waived (Last 30 Days): <strong>${dashboard.FinancialStats.FinesWaivedLast30Days:F2}</strong></p>");
        sb.AppendLine($"<p>Members With Fines: <strong>{dashboard.FinancialStats.MembersWithFines}</strong></p>");
        sb.AppendLine("</div>");
    }
    
    private void AppendOverdueReportHtml(StringBuilder sb, OverdueReportDto report)
    {
        // Summary
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>Total Overdue Items: <strong>{report.TotalCount}</strong></p>");
        sb.AppendLine($"<p>Total Estimated Fines: <strong>${report.TotalPotentialFineAmount:F2}</strong></p>");
        sb.AppendLine($"<p>Average Days Overdue: <strong>{report.AverageDaysOverdue:F1}</strong></p>");
        sb.AppendLine("</div>");
        
        // Overdue items table
        sb.AppendLine("<h2>Overdue Items</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Member</th>");
        sb.AppendLine("<th>Email</th>");
        sb.AppendLine("<th>Book Title</th>");
        sb.AppendLine("<th>Loan Date</th>");
        sb.AppendLine("<th>Due Date</th>");
        sb.AppendLine("<th>Days Overdue</th>");
        sb.AppendLine("<th>Estimated Fine</th>");
        sb.AppendLine("</tr>");
        
        foreach (var item in report.OverdueLoans)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{item.MemberName}</td>");
            sb.AppendLine($"<td>{item.MemberEmail}</td>");
            sb.AppendLine($"<td>{item.BookTitle}</td>");
            sb.AppendLine($"<td>{item.LoanDate:d}</td>");
            sb.AppendLine($"<td>{item.DueDate:d}</td>");
            sb.AppendLine($"<td>{item.DaysOverdue}</td>");
            sb.AppendLine($"<td>${item.CalculatedFine:F2}</td>");
            sb.AppendLine("</tr>");
        }
        
        sb.AppendLine("</table>");
    }
    
    private void AppendFineReportHtml(StringBuilder sb, FineReportDto report)
    {
        // Summary
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>Total Fines: <strong>{report.TotalCount}</strong></p>");
        sb.AppendLine($"<p>Total Pending Amount: <strong>${report.TotalPendingAmount:F2}</strong></p>");
        sb.AppendLine($"<p>Total Paid Amount: <strong>${report.TotalPaidAmount:F2}</strong></p>");
        sb.AppendLine($"<p>Total Waived Amount: <strong>${report.TotalWaivedAmount:F2}</strong></p>");
        sb.AppendLine($"<p>Distinct Members with Fines: <strong>{report.DistinctMembersCount}</strong></p>");
        sb.AppendLine("</div>");
        
        // Fines table
        sb.AppendLine("<h2>Fine Details</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Member</th>");
        sb.AppendLine("<th>Type</th>");
        sb.AppendLine("<th>Amount</th>");
        sb.AppendLine("<th>Date</th>");
        sb.AppendLine("<th>Status</th>");
        sb.AppendLine("<th>Book</th>");
        sb.AppendLine("<th>Description</th>");
        sb.AppendLine("</tr>");
        
        foreach (var item in report.Fines)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{item.MemberName}</td>");
            sb.AppendLine($"<td>{item.Type}</td>");
            sb.AppendLine($"<td>${item.Amount:F2}</td>");
            sb.AppendLine($"<td>{item.FineDate:d}</td>");
            sb.AppendLine($"<td>{item.Status}</td>");
            sb.AppendLine($"<td>{item.BookTitle ?? "N/A"}</td>");
            sb.AppendLine($"<td>{item.Description}</td>");
            sb.AppendLine("</tr>");
        }
        
        sb.AppendLine("</table>");
    }
    
    private void AppendOutstandingFineHtml(StringBuilder sb, OutstandingFineDto report)
    {
        // Member info and summary
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p>Member: <strong>{report.MemberName}</strong></p>");
        sb.AppendLine($"<p>Membership Status: <strong>{report.MembershipStatus}</strong></p>");
        sb.AppendLine($"<p>Outstanding Fines: <strong>{report.PendingFinesCount}</strong></p>");
        sb.AppendLine($"<p>Total Outstanding Amount: <strong>${report.OutstandingAmount:F2}</strong></p>");
        sb.AppendLine("</div>");
        
        // Outstanding fines table
        sb.AppendLine("<h2>Outstanding Fines</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Fine ID</th>");
        sb.AppendLine("<th>Type</th>");
        sb.AppendLine("<th>Amount</th>");
        sb.AppendLine("<th>Date</th>");
        sb.AppendLine("<th>Book</th>");
        sb.AppendLine("<th>Description</th>");
        sb.AppendLine("</tr>");
        
        foreach (var item in report.PendingFines)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{item.FineId}</td>");
            sb.AppendLine($"<td>{item.Type}</td>");
            sb.AppendLine($"<td>${item.Amount:F2}</td>");
            sb.AppendLine($"<td>{item.FineDate:d}</td>");
            sb.AppendLine($"<td>{item.BookTitle ?? "N/A"}</td>");
            sb.AppendLine($"<td>{item.Description}</td>");
            sb.AppendLine("</tr>");
        }
        
        sb.AppendLine("</table>");
    }
    
    #endregion
    
    #region Helper Methods
    
    private ReportType GetReportTypeFromName(string reportName)
    {
        if (Enum.TryParse<ReportType>(reportName, out var reportType))
        {
            return reportType;
        }
        
        return ReportType.Dashboard; // Default to Dashboard if parsing fails
    }
    
    #endregion
}