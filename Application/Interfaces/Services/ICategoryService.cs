using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetPaginatedCategoriesAsync(PagedRequest request, string? searchTerm = null);
    Task<CategoryDetailsDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto?> GetCategoryByNameAsync(string name);
    Task<int> CreateCategoryAsync(CreateCategoryDto categoryDto);
    Task UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto);
    Task DeleteCategoryAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> NameExistsAsync(string name);
    Task<List<CategoryDto>> GetAllCategoriesAsync();
}