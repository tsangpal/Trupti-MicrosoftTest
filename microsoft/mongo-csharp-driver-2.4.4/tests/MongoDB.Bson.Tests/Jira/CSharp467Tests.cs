/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp467Tests
    {
        public class A
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            [BsonDefaultValue("Three")]
            public string Value3 { get; set; }

            public A()
            {
                Value1 = "Default";
                Value2 = "Default";
            }
        }

        [Fact]
        public void TestOnlySpecifiedValuesAndSpecifiedDefaultValuesAreWrittenUponDeserialization()
        {
            var doc = new BsonDocument
            {
                new BsonElement("Value1", "One")
            };

            var bson = doc.ToBson();
            var rehydrated = BsonSerializer.Deserialize<A>(bson);

            Assert.Equal(rehydrated.Value1, "One");
            Assert.Equal(rehydrated.Value2, "Default");
            Assert.Equal(rehydrated.Value3, "Three");
        }
    }
}
