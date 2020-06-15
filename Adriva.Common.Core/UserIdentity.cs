using System;
using System.Security.Claims;

namespace Adriva.Common.Core {

	public class UserPrincipal : ClaimsPrincipal {

		public DateTimeOffset? ExpiresOn { get; set; } = null;

		public UserPrincipal(UserIdentity identity) {
			this.AddIdentity(identity);
		}
	}

	public class UserIdentity : ClaimsIdentity {

		public static readonly UserIdentity Empty = new UserIdentity(string.Empty, string.Empty, new Version("0.0"), null);

		public string Id => this.GetValue(ClaimTypes.NameIdentifier, this.Name);
		
		public Version Version { get; private set; }
		
		public UserIdentity(ClaimsIdentity claimsIdentity) :
			base(claimsIdentity.AuthenticationType) {
			this.AddClaim(new Claim(ClaimTypes.NameIdentifier, claimsIdentity.GetValue(ClaimTypes.NameIdentifier, string.Empty)));
			this.AddClaim(new Claim(ClaimTypes.Name, claimsIdentity.GetValue(ClaimTypes.Name, string.Empty)));
			this.AddClaim(new Claim(ClaimTypes.Version, claimsIdentity.GetValue(ClaimTypes.Version, "0.0")));
		}

		public UserIdentity(string id, string name, Version version, string authenticationType) :
			base(authenticationType) {
			this.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
			this.AddClaim(new Claim(ClaimTypes.Name, name));
			this.AddClaim(new Claim(ClaimTypes.Version, Convert.ToString(version)));
			this.Version = version;
		}
		
	}
}
