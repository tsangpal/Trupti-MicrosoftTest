// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
#if NET4 || NET45
    [Serializable]
#endif
    public class StartException : Exception
    {
        public StartException() { }
        public StartException(string message) : base(message) { }
        public StartException(string message, Exception inner) : base(message, inner) { }

#if NET4 || NET45
        protected StartException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
#endif
    }
}
