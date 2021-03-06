/* Copyright 2013-2016 MongoDB Inc.
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

using System;
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Xunit;
using MongoDB.Driver.Core.Connections;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Core.Authentication
{
    public class PlainAuthenticatorTests
    {
        private static readonly UsernamePasswordCredential __credential = new UsernamePasswordCredential("source", "user", "pencil");
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __description = new ConnectionDescription(
            new ConnectionId(__serverId),
            new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
            new BuildInfoResult(new BsonDocument("version", "2.6.0")));

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_credential_is_null()
        {
            Action act = () => new PlainAuthenticator(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)]
            bool async)
        {
            var subject = new PlainAuthenticator(__credential);

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __description, CancellationToken.None);
            }

            act.ShouldThrow<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_not_throw_when_authentication_succeeds(
            [Values(false, true)]
            bool async)
        {
            var subject = new PlainAuthenticator(__credential);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{conversationId: 0, payload: BinData(0,\"\"), done: true, ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __description, CancellationToken.None);
            }

            act.ShouldNotThrow();
            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(1);

            var actualRequestId = sentMessages[0]["requestId"].AsInt32;
            actualRequestId.Should().BeInRange(expectedRequestId, expectedRequestId + 10);

            sentMessages[0].Should().Be("{opcode: \"query\", requestId: " + actualRequestId + ", database: \"source\", collection: \"$cmd\", batchSize: -1, slaveOk: true, query: {saslStart: 1, mechanism: \"PLAIN\", payload: new BinData(0, \"AHVzZXIAcGVuY2ls\")}}");
        }
    }
}