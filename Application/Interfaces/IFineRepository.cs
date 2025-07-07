using Domain.Entities;
using Application.Common;

namespace Application.Interfaces;

public interface IFineRepository : IRepository<Fine>
{
    Task<IReadOnlyList<Fine>> GetUnpaidFinesAsync();
    Task<PagedResult<Fine>> GetUnpaidFinesPagedAsync(PagedRequest request);
    Task<decimal> GetOutstandingFinesForMemberAsync(int memberId);
    // Add more custom fine queries as needed
} 