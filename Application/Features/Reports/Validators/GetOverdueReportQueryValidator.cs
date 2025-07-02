using Application.Features.Reports.Queries;
using FluentValidation;

namespace Application.Features.Reports.Validators;

/// <summary>
/// Validator for the GetOverdueReportQuery
/// </summary>
public class GetOverdueReportQueryValidator : AbstractValidator<GetOverdueReportQuery>
{
    public GetOverdueReportQueryValidator()
    {
        // No validation required for this query as it doesn't have any parameters
        // This validator is provided for consistency and future extensions
    }
}