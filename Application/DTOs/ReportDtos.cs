using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs;

public record ReportRequestDto
{
    [Required]
    public ReportType ReportType { get; set; }

    [Required]
    public ReportFormat Format { get; set; }

    // Optional: Date range or other parameters
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? MemberId { get; set; }
    public int? BookId { get; set; }
    public int? CategoryId { get; set; }
    public bool IncludeDetails { get; set; } = false;

    // Optional: Scheduling
    public ReportScheduleDto? Schedule { get; set; }
}

public record ReportResponseDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public record ReportScheduleDto
{
    public bool IsScheduled { get; set; }
    public string? CronExpression { get; set; } // e.g., for recurring reports
    public List<string>? Recipients { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
} 