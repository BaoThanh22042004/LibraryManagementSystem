using Domain.Entities;
using System.Security.Claims;

namespace Web.Extensions
{
	public static class ClaimExtensions
	{
		public static bool TryGetUserId(this ClaimsPrincipal claimsPrincipal, out int userId)
		{
			userId = 0;
			if (claimsPrincipal == null)
			{
				return false;
			}
			var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
			{
				return false;
			}
			return true;
		}

		public static int? GetUserId(this ClaimsPrincipal claimsPrincipal)
		{
			if (claimsPrincipal == null)
			{
				return null;
			}
			var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
			{
				return null;
			}
			return userId;
		}

		public static bool TryGetFullName(this ClaimsPrincipal claimsPrincipal, out string fullName)
		{
			fullName = string.Empty;
			if (claimsPrincipal == null)
			{
				return false;
			}
			var fullNameClaim = claimsPrincipal.FindFirst(ClaimTypes.Name);
			if (fullNameClaim == null)
			{
				return false;
			}
			fullName = fullNameClaim.Value;
			return true;
		}

		public static string? GetFullName(this ClaimsPrincipal claimsPrincipal)
		{
			if (claimsPrincipal == null)
			{
				return null;
			}
			var fullNameClaim = claimsPrincipal.FindFirst(ClaimTypes.Name);
			if (fullNameClaim == null)
			{
				return null;
			}
			return fullNameClaim.Value;
		}
	}
}
