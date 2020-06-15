using Adriva.Common.Core;
using AspNet.Security.OAuth.Instagram;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Security.Claims;

namespace Adriva.Web.Core.Authentication
{

    public class InstagramAuthenticationTicketParser : IAuthenticationTicketParser
    {

        public string SchemeName => InstagramAuthenticationDefaults.AuthenticationScheme;

        public AuthenticationSchemeType Scheme => AuthenticationSchemeType.Instagram;

        public ExternalIdentity Parse(AuthenticationTicket ticket)
        {
            string id = ticket.Principal.GetValue(ClaimTypes.NameIdentifier, null);
            string firstName = ticket.Principal.GetValue(ClaimTypes.GivenName, null);
            string lastName = ticket.Principal.GetValue(ClaimTypes.Surname, null);
            string name = ticket.Principal.GetValue(ClaimTypes.Name, null);
            string username = ticket.Principal.GetValue(ExtendedClaimTypes.Username, null);
            string email = ticket.Principal.GetValue(ClaimTypes.Email, null);
            string accessToken = ticket.Properties.GetTokenValue("access_token");
            string expiresAtString = ticket.Properties.GetString(".expires");
            string profilePicture = ticket.Principal.GetValue(ExtendedClaimTypes.ProfilePicture, null);

            DateTime.TryParse(expiresAtString, out DateTime expiresAt);
            
            return new ExternalIdentity(this.Scheme)
            {
                Id = id,
                AccessToken = accessToken,
                EMail = email,
                ExpiresAt = expiresAt,
                FirstName = firstName,
                LastName = lastName,
                Username = username,
                Name = name,
                ProfilePicture = profilePicture
            };
        }
    }
}
