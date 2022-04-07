﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal class GitHubAuthority : IGitHubAuthority
    {
        /// <summary>
        /// The GitHub authorizations URL
        /// </summary>
        public const string DefaultAuthorityUrl = "https://api.github.com/authorizations";
        /// <summary>
        /// The GitHub required HTTP accepts header value
        /// </summary>
        public const string GitHubApiAcceptsHeaderValue = "application/vnd.github.v3+json";
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public GitHubAuthority(string authorityUrl = null)
        {
            _authorityUrl = authorityUrl ?? DefaultAuthorityUrl;
        }

        private readonly string _authorityUrl;

        public async Task<GitHubAuthenticationResult> AcquireToken(
            TargetUri targetUri,
            string username,
            string password,
            string authenticationCode,
            GitHubTokenScope scope)
        {
            const string GitHubOptHeader = "X-GitHub-OTP";

            Trace.WriteLine("GitHubAuthority::AcquireToken");

            Token token = null;

            using (HttpClientHandler handler = new HttpClientHandler()
            {
                MaxAutomaticRedirections = 2,
                UseDefaultCredentials = true
            })
            using (HttpClient httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
            })
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Global.GetUserAgent());
                httpClient.DefaultRequestHeaders.Add("Accept", GitHubApiAcceptsHeaderValue);

                string basicAuthValue = String.Format("{0}:{1}", username, password);
                byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
                basicAuthValue = Convert.ToBase64String(authBytes);

                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + basicAuthValue);

                if (!String.IsNullOrWhiteSpace(authenticationCode))
                {
                    httpClient.DefaultRequestHeaders.Add(GitHubOptHeader, authenticationCode);
                }

                const string HttpJsonContentType = "application/x-www-form-urlencoded";
                const string JsonContentFormat = @"{{ ""scopes"": {0}, ""note"": ""git: {1} on {2} at {3:dd-MMM-yyyy HH:mm}"" }}";

                StringBuilder scopesBuilder = new StringBuilder();
                scopesBuilder.Append('[');

                foreach (var item in scope.ToString().Split(' '))
                {
                    scopesBuilder.Append("\"")
                                 .Append(item)
                                 .Append("\"")
                                 .Append(", ");
                }

                // remove trailing ", "
                if (scopesBuilder.Length > 0)
                {
                    scopesBuilder.Remove(scopesBuilder.Length - 2, 2);
                }

                scopesBuilder.Append(']');

                string jsonContent = String.Format(JsonContentFormat, scopesBuilder, targetUri, Environment.MachineName, DateTime.Now);

                using (StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType))
                using (HttpResponseMessage response = await httpClient.PostAsync(_authorityUrl, content))
                {
                    Trace.WriteLine("   server responded with " + response.StatusCode);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                            {
                                string responseText = await response.Content.ReadAsStringAsync();

                                Match tokenMatch;
                                if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""([^""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                                    && tokenMatch.Groups.Count > 1)
                                {
                                    string tokenText = tokenMatch.Groups[1].Value;
                                    token = new Token(tokenText, TokenType.Personal);
                                }

                                if (token == null)
                                {
                                    Trace.WriteLine("   authentication failure");
                                    return new GitHubAuthenticationResult(GitHubAuthenticationResultType.Failure);
                                }
                                else
                                {
                                    Trace.WriteLine("   authentication success: new personal acces token created.");
                                    return new GitHubAuthenticationResult(GitHubAuthenticationResultType.Success, token);
                                }
                            }

                        case HttpStatusCode.Unauthorized:
                            {
                                if (String.IsNullOrWhiteSpace(authenticationCode)
                                    && response.Headers.Any(x => String.Equals(GitHubOptHeader, x.Key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    var mfakvp = response.Headers.First(x => String.Equals(GitHubOptHeader, x.Key, StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Count() > 0);

                                    if (mfakvp.Value.First().Contains("app"))
                                    {
                                        Trace.WriteLine("   two-factor app authentication code required");
                                        return new GitHubAuthenticationResult(GitHubAuthenticationResultType.TwoFactorApp);
                                    }
                                    else
                                    {
                                        Trace.WriteLine("   two-factor sms authentication code required");
                                        return new GitHubAuthenticationResult(GitHubAuthenticationResultType.TwoFactorSms);
                                    }
                                }
                                else
                                {
                                    Trace.WriteLine("   authentication failed");
                                    return new GitHubAuthenticationResult(GitHubAuthenticationResultType.Failure);
                                }
                            }

                        default:
                            Trace.WriteLine("   authentication failed");
                            return new GitHubAuthenticationResult(GitHubAuthenticationResultType.Failure);
                    }
                }
            }
        }

        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            const string ValidationUrl = "https://api.github.com/user/subscriptions";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The `targetUri` parameter is null or invalid.");
            Debug.Assert(credentials != null, "The `targetUri` parameter is null or invalid.");

            Trace.WriteLine("   GitHubAuthority::ValidateCredentials");

            string authString = String.Format("{0}:{1}", credentials.Username, credentials.Password);
            byte[] authBytes = Encoding.UTF8.GetBytes(authString);
            string authEncode = Convert.ToBase64String(authBytes);

            // craft the request header for the GitHub v3 API w/ credentials
            using (HttpClientHandler handler = new HttpClientHandler()
            {
                MaxAutomaticRedirections = 2,
                UseDefaultCredentials = true
            })
            using (HttpClient httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
            })
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Global.GetUserAgent());
                httpClient.DefaultRequestHeaders.Add("Accept", GitHubApiAcceptsHeaderValue);
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + authEncode);

                using (HttpResponseMessage response = await httpClient.GetAsync(ValidationUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Trace.WriteLine("   credential validation succeeded");
                        return true;
                    }
                    else
                    {
                        Trace.WriteLine("   credential validation failed");
                        return false;
                    }
                }
            }
        }
    }
}
