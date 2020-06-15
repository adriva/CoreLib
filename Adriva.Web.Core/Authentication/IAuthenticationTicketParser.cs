using Microsoft.AspNetCore.Authentication;

namespace Adriva.Web.Core.Authentication
{
    public interface IAuthenticationTicketParser
    {

        string SchemeName { get; }

        AuthenticationSchemeType Scheme { get; }

        ExternalIdentity Parse(AuthenticationTicket ticket);

    }
}
