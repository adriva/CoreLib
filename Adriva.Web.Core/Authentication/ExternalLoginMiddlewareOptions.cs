using System;
using System.Collections.Generic;

namespace Adriva.Web.Core.Authentication
{
    public class ExternalLoginMiddlewareOptions
    {

        private readonly IDictionary<string, IAuthenticationTicketParser> TicketParsers = new Dictionary<string, IAuthenticationTicketParser>();

        public string PathBase { get; set; }

        public string LoginPath { get; set; } = "/login";

        public string CallbackPath { get; set; } = "/callback";

        public string ReturnUrlParameter { get; set; } = "rd";

        public string SuccessPath { get; set; } = null;

        public void AddTicketParser(IAuthenticationTicketParser parser)
        {
            if (null == parser) throw new ArgumentNullException(nameof(parser));

            this.TicketParsers[parser.SchemeName] = parser;
        }

        public IAuthenticationTicketParser GetParser(string authenticationSchemeName)
        {
            if (null == authenticationSchemeName) return null;
            if (this.TicketParsers.ContainsKey(authenticationSchemeName)) return this.TicketParsers[authenticationSchemeName];
            return null;
        }
    }
}
