﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Alm.Authentication
{
    internal sealed class SecretCache : ICredentialStore, ITokenStore
    {
        public static StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

        static SecretCache()
        {
            _cache = new Dictionary<string, Secret>(KeyComparer);
        }

        private static readonly Dictionary<string, Secret> _cache;

        public SecretCache(string @namespace, Secret.UriNameConversion getTargetName = null)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(@namespace), "The namespace parameter is null or invalid");

            _namespace = @namespace;
            _getTargetName = getTargetName ?? Secret.UriToSimpleName;
        }

        private readonly string _namespace;
        private readonly Secret.UriNameConversion _getTargetName;

        /// <summary>
        /// Deletes a credential from the cache.
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being deleted</param>
        public void DeleteCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("SecretCache::DeleteCredentials");

            string targetName = this.GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName) && _cache[targetName] is Credential)
                {
                    _cache.Remove(targetName);
                }
            }
        }

        /// <summary>
        /// Deletes a token from the cache.
        /// </summary>
        /// <param name="targetUri">The key which to find and delete the token with.</param>
        public void DeleteToken(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("SecretCache::DeleteToken");

            string targetName = this.GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName) && _cache[targetName] is Token)
                {
                    _cache.Remove(targetName);
                }
            }
        }

        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being read</param>
        /// <param name="credentials">The credentials from the store; <see langword="null"/> if failure</param>
        /// <returns><see langword="true"/> if success; <see langword="false"/> if failure</returns>
        public bool ReadCredentials(TargetUri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("SecretCache::ReadCredentials");

            string targetName = this.GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName) && _cache[targetName] is Credential)
                {
                    credentials = _cache[targetName] as Credential;
                }
                else
                {
                    credentials = null;
                }
            }

            return credentials != null;
        }

        /// <summary>
        /// Gets a token from the cache.
        /// </summary>
        /// <param name="targetUri">The key which to find the token.</param>
        /// <param name="token">The token if successful; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
        public bool ReadToken(TargetUri targetUri, out Token token)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("SecretCache::ReadToken");

            string targetName = this.GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName) && _cache[targetName] is Token)
                {
                    token = _cache[targetName] as Token;
                }
                else
                {
                    token = null;
                }
            }

            return token != null;
        }

        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being stored</param>
        /// <param name="credentials">The credentials to be stored</param>
        public void WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("SecretCache::WriteCredentials");

            string targetName = this.GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName))
                {
                    _cache[targetName] = credentials;
                }
                else
                {
                    _cache.Add(targetName, credentials);
                }
            }
        }

        /// <summary>
        /// Writes a token to the cache.
        /// </summary>
        /// <param name="targetUri">The key which to index the token by.</param>
        /// <param name="token">The token to write to the cache.</param>
        public void WriteToken(TargetUri targetUri, Token token)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Token.Validate(token);

            Trace.WriteLine("SecretCache::WriteToken");

            string targetName = this.GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName))
                {
                    _cache[targetName] = token;
                }
                else
                {
                    _cache.Add(targetName, token);
                }
            }
        }

        /// <summary>
        /// Formats a TargetName string based on the TargetUri base on the format started by git-credential-winstore
        /// </summary>
        /// <param name="targetUri">Uri of the target</param>
        /// <returns>Properly formatted TargetName string</returns>
        private string GetTargetName(TargetUri targetUri)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            Trace.WriteLine("SecretCache::GetTargetName");

            return _getTargetName(targetUri, _namespace);
        }
    }
}
