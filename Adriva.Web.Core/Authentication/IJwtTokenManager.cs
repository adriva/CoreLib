using System;
using System.Security.Claims;

namespace Adriva.Web.Core
{
    public interface IJwtTokenManager
    {

        string IssueToken(ClaimsIdentity claimsIdentity, DateTime? expires);

    }
}