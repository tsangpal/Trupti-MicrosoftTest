﻿using System.Diagnostics;

namespace Microsoft.Alm.Authentication.Test
{
    public class AuthenticationTests
    {
        public static readonly Credential DefaultCredentials = new Credential("username", "password");
        public static readonly Token DefaultAzureAccessToken = new Token("azure-access-token", TokenType.Test);
        public static readonly Token DefaultAzureRefreshToken = new Token("azure-refresh-token", TokenType.Test);
        public static readonly Credential DefaultPersonalAccessToken = new Credential("personal-access-token", "personal-access-token");
        public static readonly TargetUri DefaultTargetUri = new TargetUri("https://unit-test.uri/git-credential");
        public static readonly TargetUri InvalidTargetUri = new TargetUri("https://invlaid-test.uri/git-credential");
        public static readonly VstsTokenScope DefaultTokenScope = VstsTokenScope.CodeWrite;

        public AuthenticationTests()
        {
            if (Trace.Listeners.Count == 0)
            {
                Trace.Listeners.AddRange(Debug.Listeners);
            }
        }
    }
}
