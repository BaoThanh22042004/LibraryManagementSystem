using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reports.Queries;

/// <summary>
/// Query to retrieve a comprehensive report of overdue loans
/// </summary>
public record GetOverdueReportQuery : IRequest<Result<OverdueReportDto>>;

public class GetOverdueReportQueryHandler : IRequestHandler<GetOverdueReportQuery, Result<OverdueReportDto>>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;
	private readonly decimal _dailyFineRate = 0.50m; // $0.50 per day overdue

	public GetOverdueReportQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<Result<OverdueReportDto>> Handle(GetOverdueReportQuery request, CancellationToken cancellationToken)
	{
		try
		{
			// Get current date for overdue calculation
			var today = DateTime.Now.Date;

			// Get all loans that are overdue (due date passed and status is Active or Overdue)
			var overdueLoans = await _unitOfWork.Repository<Loan>().ListAsync(
				predicate: l => l.DueDate < today && (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue),
				orderBy: q => q.OrderByDescending(l => l.DueDate),
				includes: [
					l => l.Member,
					l => l.BookCopy
				]);

			// Process each overdue loan
			var overdueLoanDtos = new List<OverdueLoanDto>();
			int totalDaysOverdue = 0;
			decimal totalFineAmount = 0;

			foreach (var loan in overdueLoans)
			{
				// Calculate days overdue
				int daysOverdue = (today - loan.DueDate.Date).Days;

				// Calculate potential fine amount
				decimal fineAmount = daysOverdue * _dailyFineRate;

				// Track totals for report summary
				totalDaysOverdue += daysOverdue;
				totalFineAmount += fineAmount;

				// Map to DTO
				var loanDto = _mapper.Map<OverdueLoanDto>(loan);

				overdueLoanDtos.Add(loanDto);
			}

			// Create report DTO
			var reportDto = new OverdueReportDto
			{
				OverdueLoans = overdueLoanDtos,
				GeneratedAt = DateTime.Now,
			};

			return Result.Success(reportDto);
		}
		catch (Exception ex)
		{
			return Result.Failure<OverdueReportDto>($"Error generating overdue report: {ex.Message}");
		}
	}
}