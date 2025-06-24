using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface ILibrarianService
{
    Task<PagedResult<LibrarianDto>> GetPaginatedLibrariansAsync(PagedRequest request, string? searchTerm = null);
    Task<LibrarianDto?> GetLibrarianByIdAsync(int id);
    Task<LibrarianDto?> GetLibrarianByEmployeeIdAsync(string employeeId);
    Task<LibrarianDto?> GetLibrarianByUserIdAsync(int userId);
    Task<int> CreateLibrarianAsync(CreateLibrarianDto librarianDto);
    Task UpdateLibrarianAsync(int id, UpdateLibrarianDto librarianDto);
    Task DeleteLibrarianAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> EmployeeIdExistsAsync(string employeeId);
}