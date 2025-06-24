using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IMemberService
{
    Task<PagedResult<MemberDto>> GetPaginatedMembersAsync(PagedRequest request, string? searchTerm = null);
    Task<MemberDetailsDto?> GetMemberByIdAsync(int id);
    Task<MemberDto?> GetMemberByMembershipNumberAsync(string membershipNumber);
    Task<MemberDto?> GetMemberByUserIdAsync(int userId);
    Task<int> CreateMemberAsync(CreateMemberDto memberDto);
    Task UpdateMemberAsync(int id, UpdateMemberDto memberDto);
    Task DeleteMemberAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> MembershipNumberExistsAsync(string membershipNumber);
    Task<decimal> GetOutstandingFinesAsync(int memberId);
    Task<bool> UpdateMembershipStatusAsync(int memberId, Domain.Enums.MembershipStatus status);
}