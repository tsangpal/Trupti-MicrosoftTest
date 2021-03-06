// -----------------------------------------------------------------------------------------
// <copyright file="FileContext.cs" company="Microsoft">
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
// -----------------------------------------------------------------------------------------

using Microsoft.WindowsAzure.Storage.Auth;
using System;

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    internal class FileContext
    {
        public int Timeout { get; private set; }

        public bool Async { get; private set; }

        public string Account
        {
            get
            {
                return owner ? TestBase.TargetTenantConfig.AccountName : null;
            }
        }

        public byte[] Key
        {
            get
            {
                return owner ? Convert.FromBase64String(TestBase.TargetTenantConfig.AccountKey) : null;
            }
        }

        public StorageCredentials Credentials
        {
            get
            {
                return owner ? new StorageCredentials(Account, Key) : null;
            }
        }

        public string Address
        {
            get
            {
                return TestBase.TargetTenantConfig.FileServiceEndpoint;
            }
        }

        private bool owner;

        public FileContext(bool owner, bool async, int timeout)
        {
            this.Timeout = timeout;
            this.owner = owner;
            this.Async = async;
        }
    }
}
