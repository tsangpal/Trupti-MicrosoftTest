// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
namespace System
{
    public class SystemException : Exception
    {
        public SystemException() {}
        public SystemException(string message) : base(message) {}
        public SystemException(string message, Exception innerException) : base(message, innerException) {}
    }
}