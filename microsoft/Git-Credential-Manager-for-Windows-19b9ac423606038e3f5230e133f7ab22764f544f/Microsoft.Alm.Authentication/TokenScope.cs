﻿using System;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.Alm.Authentication
{
    public abstract class TokenScope : IEquatable<TokenScope>
    {
        protected TokenScope(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                _scopes = new string[0];
            }
            else
            {
                _scopes = new string[1];
                _scopes[0] = value;
            }
        }

        protected TokenScope(string[] values)
        {
            _scopes = values;
        }

        protected TokenScope(ScopeSet set)
        {
            string[] result = new string[set.Count];
            set.CopyTo(result);

            _scopes = result;
        }

        public string Value { get { return String.Join(" ", _scopes); } }

        protected readonly string[] _scopes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return this == obj as TokenScope;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TokenScope other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            // largest 31-bit prime (https://msdn.microsoft.com/en-us/library/Ee621251.aspx)
            int hash = 2147483647;

            for (int i = 0; i < _scopes.Length; i++)
            {
                unchecked
                {
                    hash ^= _scopes[i].GetHashCode();
                }
            }

            return hash;
        }

        public override String ToString()
        {
            return Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TokenScope scope1, TokenScope scope2)
        {
            if (ReferenceEquals(scope1, scope2))
                return true;
            if (ReferenceEquals(scope1, null) || ReferenceEquals(null, scope2))
                return false;

            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            return set.SetEquals(scope2._scopes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TokenScope scope1, TokenScope scope2)
        {
            return !(scope1 == scope2);
        }
    }
}
