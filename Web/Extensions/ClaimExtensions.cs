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
	}
}
