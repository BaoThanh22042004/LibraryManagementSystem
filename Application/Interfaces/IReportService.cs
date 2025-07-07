using System.Threading.Tasks;
using Application.DTOs;
using Application.Common;

namespace Application.Interfaces;

public interface IReportService
{
    Task<Result<ReportResponseDto>> GenerateReportAsync(ReportRequestDto request);
    // Optionally: Add methods for scheduling, listing, or managing reports
} 