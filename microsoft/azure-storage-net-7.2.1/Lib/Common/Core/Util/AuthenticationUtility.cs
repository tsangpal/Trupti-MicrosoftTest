//-----------------------------------------------------------------------
// <copyright file="AuthenticationUtility.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Net;
    using System.Text;
#if WINDOWS_RT || NETCORE
    using System.Net.Http;
#endif

    internal static class AuthenticationUtility
    {
        private const int ExpectedResourceStringLength = 100;
        private const int ExpectedHeaderNameAndValueLength = 50;
        private const char HeaderNameValueSeparator = ':';
        private const char HeaderValueDelimiter = ',';

#if WINDOWS_RT || NETCORE
        /// <summary>
        /// Gets the value of the x-ms-date or Date header.
        /// </summary>
        /// <param name="request">The request where the value is read from.</param>
        /// <returns>The value of the x-ms-date or Date header.</returns>
        public static string GetPreferredDateHeaderValue(HttpRequestMessage request)
        {
            string microsoftDateHeaderValue = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(request.Headers, Constants.HeaderConstants.Date);
            if (!string.IsNullOrEmpty(microsoftDateHeaderValue))
            {
                return microsoftDateHeaderValue;
            }

            return AuthenticationUtility.GetCanonicalizedHeaderValue(request.Headers.Date);
        }

        /// <summary>
        /// Appends the value of the Content-Length header to the specified canonicalized string.
        /// </summary>
        /// <param name="canonicalizedString">The canonicalized string where the value is appended.</param>
        /// <param name="request">The request where the value is read from.</param>
        public static void AppendCanonicalizedContentLengthHeader(CanonicalizedString canonicalizedString, HttpRequestMessage request)
        {
            long? contentLength = request.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value != -1L && contentLength.Value != 0)
            {
                canonicalizedString.AppendCanonicalizedElement(contentLength.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                canonicalizedString.AppendCanonicalizedElement(null);
            }
        }

        /// <summary>
        /// Appends the value of the Date header (or, optionally, the x-ms-date header) to the specified canonicalized string.
        /// </summary>
        /// <param name="canonicalizedString">The canonicalized string where the value is appended.</param>
        /// <param name="request">The request where the value is read from.</param>
        /// <param name="allowMicrosoftDateHeader">true if the value of the x-ms-date header can be used and is preferred; otherwise, false.</param>
        public static void AppendCanonicalizedDateHeader(CanonicalizedString canonicalizedString, HttpRequestMessage request, bool allowMicrosoftDateHeader = false)
        {
            string microsoftDateHeaderValue = HttpResponseMessageUtils.GetHeaderSingleValueOrDefault(request.Headers, Constants.HeaderConstants.Date);
            if (string.IsNullOrEmpty(microsoftDateHeaderValue))
            {
                canonicalizedString.AppendCanonicalizedElement(AuthenticationUtility.GetCanonicalizedHeaderValue(request.Headers.Date));
            }
            else if (allowMicrosoftDateHeader)
            {
                canonicalizedString.AppendCanonicalizedElement(microsoftDateHeaderValue);
            }
            else
            {
                canonicalizedString.AppendCanonicalizedElement(null);
            }
        }

        /// <summary>
        /// Appends the values of the x-ms-* headers to the specified canonicalized string.
        /// </summary>
        /// <param name="canonicalizedString">The canonicalized string where the values are appended.</param>
        /// <param name="request">The request where the values are read from.</param>
        public static void AppendCanonicalizedCustomHeaders(CanonicalizedString canonicalizedString, HttpRequestMessage request)
        {
            CultureInfo sortingCulture = new CultureInfo("en-US");
            StringComparer sortingComparer = new CultureStringComparer(sortingCulture, false);
            SortedDictionary<string, IEnumerable<string>> headers = new SortedDictionary<string, IEnumerable<string>>(sortingComparer);

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                string headerName = header.Key;
                if (headerName.StartsWith(Constants.HeaderConstants.PrefixForStorageHeader, StringComparison.OrdinalIgnoreCase))
                {
                    headers.Add(headerName.ToLowerInvariant(), header.Value);
                }
            }

            if (request.Content != null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                {
                    string headerName = header.Key;
                    if (headerName.StartsWith(Constants.HeaderConstants.PrefixForStorageHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        headers.Add(headerName.ToLowerInvariant(), header.Value);
                    }
                }
            }

            StringBuilder canonicalizedElement = new StringBuilder(ExpectedHeaderNameAndValueLength);
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                canonicalizedElement.Clear();
                canonicalizedElement.Append(header.Key);
                canonicalizedElement.Append(HeaderNameValueSeparator);
                int keyLength = canonicalizedElement.Length;

                foreach (string value in header.Value)
                {
                    canonicalizedElement.Append(value.TrimStart().Replace("\r\n", string.Empty));
                    canonicalizedElement.Append(HeaderValueDelimiter);
                }
                
                // If the delta is 0 or 1, then the metadata value was null or empty so we should not include it.
                if (canonicalizedElement.Length - keyLength > 1)
                {
                    canonicalizedString.AppendCanonicalizedElement(canonicalizedElement.ToString(0, canonicalizedElement.Length - 1));
                }
            }
        }
#else
        /// <summary>
        /// Gets the value of the x-ms-date or Date header.
        /// </summary>
        /// <param name="request">The request where the value is read from.</param>
        /// <returns>The value of the x-ms-date or Date header.</returns>
        public static string GetPreferredDateHeaderValue(HttpWebRequest request)
        {
            string microsoftDateHeaderValue = request.Headers[Constants.HeaderConstants.Date];
            if (!string.IsNullOrEmpty(microsoftDateHeaderValue))
            {
                return microsoftDateHeaderValue;
            }

            return request.Headers[HttpRequestHeader.Date];
        }

        /// <summary>
        /// Appends the value of the Content-Length header to the specified canonicalized string.
        /// </summary>
        /// <param name="canonicalizedString">The canonicalized string where the value is appended.</param>
        /// <param name="request">The request where the value is read from.</param>
        public static void AppendCanonicalizedContentLengthHeader(CanonicalizedString canonicalizedString, HttpWebRequest request)
        {
            if (request.ContentLength != -1L && request.ContentLength != 0)
            {
                canonicalizedString.AppendCanonicalizedElement(request.ContentLength.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                canonicalizedString.AppendCanonicalizedElement(null);
            }
        }

        /// <summary>
        /// Appends the value of the Date header (or, optionally, the x-ms-date header) to the specified canonicalized string.
        /// </summary>
        /// <param name="canonicalizedString">The canonicalized string where the value is appended.</param>
        /// <param name="request">The request where the value is read from.</param>
        /// <param name="allowMicrosoftDateHeader">true if the value of the x-ms-date header can be used and is preferred; otherwise, false.</param>
        public static void AppendCanonicalizedDateHeader(CanonicalizedString canonicalizedString, HttpWebRequest request, bool allowMicrosoftDateHeader = false)
        {
            string microsoftDateHeaderValue = request.Headers[Constants.HeaderConstants.Date];
            if (string.IsNullOrEmpty(microsoftDateHeaderValue))
            {
                canonicalizedString.AppendCanonicalizedElement(request.Headers[HttpRequestHeader.Date]);
            }
            else if (allowMicrosoftDateHeader)
            {
                canonicalizedString.AppendCanonicalizedElement(microsoftDateHeaderValue);
            }
            else
            {
                canonicalizedString.AppendCanonicalizedElement(null);
            }
        }

        /// <summary>
        /// Appends the values of the x-ms-* headers to the specified canonicalized string.
        /// </summary>
        /// <param name="canonicalizedString">The canonicalized string where the values are appended.</param>
        /// <param name="request">The request where the values are read from.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Reviewed.")]
        public static void AppendCanonicalizedCustomHeaders(CanonicalizedString canonicalizedString, HttpWebRequest request)
        {
            List<string> headerNames = new List<string>(request.Headers.AllKeys.Length);
            foreach (string headerName in request.Headers.AllKeys)
            {
                if (headerName.StartsWith(Constants.HeaderConstants.PrefixForStorageHeader, StringComparison.OrdinalIgnoreCase))
                {
                    headerNames.Add(headerName.ToLowerInvariant());
                }
            }

            CultureInfo sortingCulture = new CultureInfo("en-US");
            StringComparer sortingComparer = StringComparer.Create(sortingCulture, false);
            headerNames.Sort(sortingComparer);

            StringBuilder canonicalizedElement = new StringBuilder(ExpectedHeaderNameAndValueLength);
            foreach (string headerName in headerNames)
            {
                string value = request.Headers[headerName];
                if (!string.IsNullOrEmpty(value))
                {
                    canonicalizedElement.Length = 0;
                    canonicalizedElement.Append(headerName);
                    canonicalizedElement.Append(HeaderNameValueSeparator);
                    canonicalizedElement.Append(value.TrimStart().Replace("\r\n", string.Empty));

                    canonicalizedString.AppendCanonicalizedElement(canonicalizedElement.ToString());
                }
            }
        }
#endif

        /// <summary>
        /// Gets the canonicalized header value to use for the specified date/time or <c>null</c> if it does not have a value.
        /// </summary>
        /// <param name="value">The date/time.</param>
        /// <returns>The canonicalized header value to use for the specified date/time or <c>null</c> if it does not have a value.</returns>
        public static string GetCanonicalizedHeaderValue(DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                return HttpWebUtility.ConvertDateTimeToHttpString(value.Value);
            }

            return null;
        }

        /// <summary>
        /// In case of path style, this method will strip off -secondary from absolute path and replace it with account name.
        /// </summary>
        /// <param name="uri">The resource URI.</param>
        /// <param name="accountName">The name of the storage account.</param>
        /// <returns>Absolute path with no -secondary suffix.</returns>
        private static string GetAbsolutePathWithoutSecondarySuffix(Uri uri, string accountName)
        {
            string absolutePath = uri.AbsolutePath;
            string secondaryAccountName = string.Concat(accountName, CloudStorageAccount.SecondaryLocationAccountSuffix);
            
            int startIndex = absolutePath.IndexOf(secondaryAccountName, StringComparison.OrdinalIgnoreCase);
            if (startIndex == 1)
            {
                startIndex += accountName.Length;
                absolutePath = absolutePath.Remove(startIndex, CloudStorageAccount.SecondaryLocationAccountSuffix.Length);
            }

            return absolutePath;
        }

        /// <summary>
        /// Gets the canonicalized resource string for the specified URI.
        /// </summary>
        /// <param name="uri">The resource URI.</param>
        /// <param name="accountName">The name of the storage account.</param>
        /// <param name="isSharedKeyLiteOrTableService">true when using the Shared Key Lite authentication scheme or the Table service; otherwise, false.</param>
        /// <returns>The canonicalized resource string.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Reviewed.")]
        public static string GetCanonicalizedResourceString(Uri uri, string accountName, bool isSharedKeyLiteOrTableService = false)
        {
            StringBuilder canonicalizedResource = new StringBuilder(ExpectedResourceStringLength);
            canonicalizedResource.Append('/');
            canonicalizedResource.Append(accountName);
            canonicalizedResource.Append(GetAbsolutePathWithoutSecondarySuffix(uri, accountName));

            IDictionary<string, string> queryParameters = HttpWebUtility.ParseQueryString(uri.Query);
            if (!isSharedKeyLiteOrTableService)
            {
                List<string> queryParameterNames = new List<string>(queryParameters.Keys);
                queryParameterNames.Sort(StringComparer.OrdinalIgnoreCase);

                foreach (string queryParameterName in queryParameterNames)
                {
                    canonicalizedResource.Append('\n');
                    canonicalizedResource.Append(queryParameterName.ToLowerInvariant());
                    canonicalizedResource.Append(':');
                    canonicalizedResource.Append(queryParameters[queryParameterName]);
                }
            }
            else
            {
                // Add only the comp parameter
                string compQueryParameterValue;
                if (queryParameters.TryGetValue("comp", out compQueryParameterValue))
                {
                    canonicalizedResource.Append("?comp=");
                    canonicalizedResource.Append(compQueryParameterValue);
                }
            }

            return canonicalizedResource.ToString();
        }
    }
}
