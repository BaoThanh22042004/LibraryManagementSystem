using System.Security.Claims;
using Domain.Enums;

namespace Application.Common.Extensions;

/// <summary>
/// Extension methods for working with claims and user context
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from claims
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Gets the user role from claims
    /// </summary>
    public static UserRole GetUserRole(this ClaimsPrincipal principal)
    {
        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(roleClaim?.Value, out var role) ? role : UserRole.Member;
    }

    /// <summary>
    /// Gets the user email from claims
    /// </summary>
    public static string GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Gets the user full name from claims
    /// </summary>
    public static string GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Gets the membership number from claims (if applicable)
    /// </summary>
    public static string? GetMembershipNumber(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("membershipNumber")?.Value;
    }

    /// <summary>
    /// Checks if the user is in a specific role
    /// </summary>
    public static bool IsInRole(this ClaimsPrincipal principal, UserRole role)
    {
        return principal.GetUserRole() == role;
    }

    /// <summary>
    /// Checks if the user is an admin
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(UserRole.Admin);
    }

    /// <summary>
    /// Checks if the user is a librarian
    /// </summary>
    public static bool IsLibrarian(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(UserRole.Librarian);
    }

    /// <summary>
    /// Checks if the user is a member
    /// </summary>
    public static bool IsMember(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(UserRole.Member);
    }

    /// <summary>
    /// Checks if the user is staff (Admin or Librarian)
    /// </summary>
    public static bool IsStaff(this ClaimsPrincipal principal)
    {
        var role = principal.GetUserRole();
        return role == UserRole.Admin || role == UserRole.Librarian;
    }

    /// <summary>
    /// Checks if the user can manage other users
    /// </summary>
    public static bool CanManageUsers(this ClaimsPrincipal principal)
    {
        return principal.IsStaff();
    }

    /// <summary>
    /// Checks if the user can manage a specific user type
    /// </summary>
    public static bool CanManageUserType(this ClaimsPrincipal principal, UserRole targetRole)
    {
        var currentRole = principal.GetUserRole();
        
        // Members cannot manage any users
        if (currentRole == UserRole.Member)
            return false;
        
        // Librarians can only manage Members
        if (currentRole == UserRole.Librarian)
            return targetRole == UserRole.Member;
        
        // Admins can manage all user types
        return currentRole == UserRole.Admin;
    }
}

/// <summary>
/// Extension methods for working with validation results
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Converts FluentValidation results to Result
    /// </summary>
    public static Common.Result ToResult(this FluentValidation.Results.ValidationResult validationResult)
    {
        if (validationResult.IsValid)
            return Common.Result.Success();

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return Common.Result.Failure(errors);
    }

    /// <summary>
    /// Converts FluentValidation results to Result<T>
    /// </summary>
    public static Common.Result<T> ToResult<T>(this FluentValidation.Results.ValidationResult validationResult, T value)
    {
        if (validationResult.IsValid)
            return Common.Result.Success(value);

        var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
        return Common.Result.Failure<T>(errors);
    }
}

/// <summary>
/// Extension methods for working with DateTime
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Checks if a date is within the last specified hours
    /// </summary>
    public static bool IsWithinLastHours(this DateTime dateTime, int hours)
    {
        return dateTime >= DateTime.UtcNow.AddHours(-hours);
    }

    /// <summary>
    /// Checks if a date is within the last specified days
    /// </summary>
    public static bool IsWithinLastDays(this DateTime dateTime, int days)
    {
        return dateTime >= DateTime.UtcNow.AddDays(-days);
    }

    /// <summary>
    /// Gets the age in years from a birth date
    /// </summary>
    public static int GetAge(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        
        if (birthDate.Date > today.AddYears(-age))
            age--;
        
        return age;
    }

    /// <summary>
    /// Formats a DateTime for display
    /// </summary>
    public static string ToDisplayString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Formats a DateTime for display (nullable)
    /// </summary>
    public static string ToDisplayString(this DateTime? dateTime)
    {
        return dateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
    }
}

/// <summary>
/// Extension methods for working with enums
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the display name for an enum value
    /// </summary>
    public static string GetDisplayName(this Enum enumValue)
    {
        return enumValue.ToString().Replace("_", " ");
    }

    /// <summary>
    /// Gets all enum values as a dictionary
    /// </summary>
    public static Dictionary<int, string> GetValueNamePairs<T>() where T : struct, Enum
    {
        return Enum.GetValues<T>()
            .ToDictionary(e => Convert.ToInt32(e), e => e.GetDisplayName());
    }
}

/// <summary>
/// Extension methods for working with collections
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Checks if a collection is null or empty
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection == null || !collection.Any();
    }

    /// <summary>
    /// Safely gets an item from a collection by index
    /// </summary>
    public static T? SafeGet<T>(this IList<T> list, int index)
    {
        return index >= 0 && index < list.Count ? list[index] : default;
    }

    /// <summary>
    /// Splits a collection into chunks of specified size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be greater than 0", nameof(size));

        var list = source.ToList();
        for (int i = 0; i < list.Count; i += size)
        {
            yield return list.Skip(i).Take(size);
        }
    }
}
