using Application.Features.Reports.Queries;
using FluentValidation;

namespace Application.Features.Reports.Validators;

/// <summary>
/// Validator for the GetFinesReportQuery
/// </summary>
public class GetFinesReportQueryValidator : AbstractValidator<GetFinesReportQuery>
{
    public GetFinesReportQueryValidator()
    {
        // No specific validation required for this query
        // The includeStatus parameter is optional and can be null
    }
}

/// <summary>
/// Validator for the GetOutstandingFinesQuery
/// </summary>
public class GetOutstandingFinesQueryValidator : AbstractValidator<GetOutstandingFinesQuery>
{
    public GetOutstandingFinesQueryValidator()
    {
        RuleFor(q => q.MemberId)
            .GreaterThan(0)
            .WithMessage("Member ID must be greater than 0");
    }
}

/// <summary>
/// Validator for the HasOutstandingFinesQuery
/// </summary>
public class HasOutstandingFinesQueryValidator : AbstractValidator<HasOutstandingFinesQuery>
{
    public HasOutstandingFinesQueryValidator()
    {
        RuleFor(q => q.MemberId)
            .GreaterThan(0)
            .WithMessage("Member ID must be greater than 0");
    }
}

/// <summary>
/// Validator for the GetTotalOutstandingFineAmountQuery
/// </summary>
public class GetTotalOutstandingFineAmountQueryValidator : AbstractValidator<GetTotalOutstandingFineAmountQuery>
{
    public GetTotalOutstandingFineAmountQueryValidator()
    {
        RuleFor(q => q.MemberId)
            .GreaterThan(0)
            .WithMessage("Member ID must be greater than 0");
    }
}