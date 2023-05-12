using System;
using System.Collections.Generic;
using System.Text;

namespace Adriva.Web.Core.Authentication
{
    public enum AuthenticationSchemeType
    {
        Unknown = 0,
        LocalCookie = 1,
        LocalToken = 2,
        Google = 3,
        Instagram = 4,
        Facebook = 5,
        Microsoft = 6
    }
}
