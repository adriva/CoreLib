using System.Security.Claims;

namespace Adriva.Common.Core {
	public static class ClaimsExtension {

		public static string GetValue(this ClaimsIdentity identity, string type, string defaultValue) {
			Claim claim = identity.FindFirst(type);
			if (null == claim) return defaultValue;
			return claim.Value;
		}


		public static string GetId(this ClaimsIdentity identity) {
			return identity.GetValue(ClaimTypes.NameIdentifier, string.Empty);
		}

        public static string GetValue(this ClaimsPrincipal principal, string type, string defaultValue)
        {
            Claim claim = principal.FindFirst(type);
            if (null == claim) return defaultValue;
            return claim.Value;
        }

        public static bool IsGuest(this ClaimsIdentity identity)
        {
            return null != identity.GetValue(ClaimTypes.Anonymous, null);
        }
	}

    public static class ExtendedClaimTypes
    {
        public static readonly string ProfilePicture = "adriva:profilePicture";

        public static readonly string Username = "adriva:username";
    }
}
