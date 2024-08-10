using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Adriva.Web.Core
{
    public class DefaultJwtTokenManager : IJwtTokenManager
    {
        private readonly SymmetricSecurityKey SecurityKey;

        public DefaultJwtTokenManager(SymmetricSecurityKey securityKey)
        {
            this.SecurityKey = securityKey ?? throw new ArgumentNullException(nameof(securityKey));
        }

        public string IssueToken(ClaimsIdentity claimsIdentity, DateTime? expires)
        {
            if (null == claimsIdentity) throw new ArgumentNullException(nameof(claimsIdentity));
            if (!claimsIdentity.Claims.Any()) throw new ArgumentException("Identity doesn't have any claims");

            JwtSecurityToken token = new JwtSecurityToken(
                claims: claimsIdentity.Claims,
                expires: expires,
                signingCredentials: new SigningCredentials(this.SecurityKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}