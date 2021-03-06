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
    public class CSharp81Tests
    {
        private class BaseModel
        {
            [BsonId]
            public ObjectId Id { get; set; }
        }

        private class User : BaseModel
        {
            public ObjectId FriendId { get; set; }
        }

        [Fact]
        public void TestIdMember()
        {
            var u = new User { Id = ObjectId.Empty, FriendId = ObjectId.Empty };

            var classMap = BsonClassMap.LookupClassMap(typeof(User));
            var idMemberMap = classMap.IdMemberMap;
            Assert.Equal("Id", idMemberMap.MemberName);

            var idProvider = BsonSerializer.LookupSerializer(typeof(User)) as IBsonIdProvider;
            var idGenerator = BsonSerializer.LookupIdGenerator(typeof(ObjectId));
            idProvider.SetDocumentId(u, idGenerator.GenerateId(null, u));
            Assert.False(idGenerator.IsEmpty(u.Id));
            Assert.True(idGenerator.IsEmpty(u.FriendId));
        }
    }
}