// -----------------------------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="Microsoft">
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

#if WINDOWS_DESKTOP
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
namespace Microsoft.WindowsAzure.Storage.Table.Entities
{
    public class BaseEntity : TableEntity
    {
        public BaseEntity()
        {
        }

        public BaseEntity(string pk, string rk)
            : base(pk, rk)
        {
        }

        public void Populate()
        {
            this.foo = "bar";
            this.A = "a";
            this.B = "b";
            this.C = "c";
            this.D = "d";
            this.E = 1234;
        }

        public string foo { get; set; }
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public int E { get; set; }

        public void Validate()
        {
            Assert.AreEqual(this.foo, "bar");
            Assert.AreEqual(this.A, "a");
            Assert.AreEqual(this.B, "b");
            Assert.AreEqual(this.C, "c");
            Assert.AreEqual(this.D, "d");
            Assert.AreEqual(this.E, 1234);
        }

        public static EdmType BaseEntityPropertyResolver(string partitionKey, string rowKey, string propName, string propValue)
        {
            if (propName == "E")
            {
                return EdmType.Int32;
            }

            return EdmType.String;
        }
    }
}
