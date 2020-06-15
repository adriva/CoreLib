using AspNet.Security.OAuth.Instagram;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using System;

namespace Adriva.Web.Core.Authentication
{

    public class AuthenticationOptions
    {
        public bool UseCookies { get; set; }

        public bool UseGoogle { get; set; }

        public bool UseInstagram { get; set; }

        public bool UseJwtBearer { get; set; }

        public Action<CookieAuthenticationOptions> ConfigureCookie { get; set; }

        public Action<GoogleOptions> ConfigureGoogle { get; set; }

        public Action<InstagramAuthenticationOptions> ConfigureInstagram { get; set; }

        public Action<JwtBearerOptions> ConfigureJwtBearer { get; set; }
    }
}
