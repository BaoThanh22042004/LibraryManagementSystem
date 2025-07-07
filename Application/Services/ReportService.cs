using System.Text;
using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
// TODO: Move CSV export logic to a shared library or inject/export via service for proper cross-layer usage.

namespace Application.Services;

public class ReportService : IReportService
{
    private readonly ILoanService _loanService;
    private readonly IFineService _fineService;
    private readonly IAuditService _auditService;
    // Add other dependencies as needed

    public ReportService(ILoanService loanService, IFineService fineService, IAuditService auditService)
    {
        _loanService = loanService;
        _fineService = fineService;
        _auditService = auditService;
    }

    public async Task<Result<ReportResponseDto>> GenerateReportAsync(ReportRequestDto request)
    {
        // var userId = request.Schedule?.Recipients?.FirstOrDefault(); // Placeholder: pass userId as needed
        var reportType = request.ReportType.ToString();
        var format = request.Format.ToString();
        try
        {
            Result<ReportResponseDto> result;
            switch (request.ReportType)
            {
                case ReportType.OverdueLoans:
                    result = await GenerateOverdueLoansReport(request);
                    break;
                case ReportType.Fines:
                    result = await GenerateFinesReport(request);
                    break;
                default:
                    result = Result.Failure<ReportResponseDto>("Unsupported report type.");
                    break;
            }
            // Audit log removed: now handled in controller
            return result;
        }
        catch
        {
            // Audit log removed: now handled in controller
            throw;
        }
    }

    private async Task<Result<ReportResponseDto>> GenerateOverdueLoansReport(ReportRequestDto request)
    {
        var result = await _loanService.GetOverdueLoansReportAsync();
        if (!result.IsSuccess)
            return Result.Failure<ReportResponseDto>(result.Error);
        // Only CSV for now
        var csv = CsvExportExtensions.ToCsv(result.Value);
        return Result.Success(new ReportResponseDto
        {
            Content = Encoding.UTF8.GetBytes(csv),
            ContentType = "text/csv",
            FileName = $"OverdueLoans_{DateTime.UtcNow:yyyyMMdd}.csv"
        });
    }

    private async Task<Result<ReportResponseDto>> GenerateFinesReport(ReportRequestDto request)
    {
        var result = await _fineService.GetFinesReportAsync();
        if (!result.IsSuccess)
            return Result.Failure<ReportResponseDto>(result.Error);
        // Only CSV for now
        var csv = CsvExportExtensions.ToCsv(result.Value);
        return Result.Success(new ReportResponseDto
        {
            Content = Encoding.UTF8.GetBytes(csv),
            ContentType = "text/csv",
            FileName = $"FinesReport_{DateTime.UtcNow:yyyyMMdd}.csv"
        });
    }
} 