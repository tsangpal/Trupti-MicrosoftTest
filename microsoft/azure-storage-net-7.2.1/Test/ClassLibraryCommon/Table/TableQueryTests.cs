// -----------------------------------------------------------------------------------------
// <copyright file="TableQueryTests.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table.Entities;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableQueryTests : TableTestBase
    {
        readonly CloudTableClient DefaultTableClient = new CloudTableClient(new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint), TestBase.StorageCredentials);

        #region Locals + Ctors
        public TableQueryTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        static CloudTable currentTable = null;

        static CloudTableClient tableClient = null;

        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            tableClient = GenerateCloudTableClient();
            currentTable = tableClient.GetTableReference(GenerateRandomTableName());
            currentTable.CreateIfNotExists();

            for (int i = 0; i < 15; i++)
            {
                TableBatchOperation batch = new TableBatchOperation();

                for (int j = 0; j < 100; j++)
                {
                    DynamicTableEntity ent = GenerateRandomEntity("tables_batch_" + i.ToString());
                    ent.RowKey = string.Format("{0:0000}", j);
                    batch.Insert(ent);
                }

                currentTable.ExecuteBatch(batch);
            }
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            currentTable.DeleteIfExists();
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.TableBufferManager != null)
            {
                TestBase.TableBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.TableBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.TableBufferManager.OutstandingBufferCount);
            }
        }

        #endregion

        #region Unit Tests
        #region Query Segmented

        #region Sync
        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryBasicSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryBasicSync(payloadFormat);
            }
        }

        private void DoTableQueryBasicSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));
            query.TakeCount = 100;

            TableQuerySegment<DynamicTableEntity> seg = currentTable.ExecuteQuerySegmented(query, null);

            foreach (DynamicTableEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.Properties.Count, 4);
            }

            Assert.AreEqual(100, query.TakeCount);
        }

        [TestMethod]
        [Description("A test to validate basic table continuation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithContinuationSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryWithContinuationSync(payloadFormat);
            }
        }

        private void DoTableQueryWithContinuationSync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery();

            OperationContext opContext = new OperationContext();
            TableQuerySegment<DynamicTableEntity> seg = currentTable.ExecuteQuerySegmented(query, null, null, opContext);

            int count = 0;
            foreach (DynamicTableEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            seg = currentTable.ExecuteQuerySegmented(query, seg.ContinuationToken, null, opContext);
            seg.ContinuationToken = null;

            foreach (DynamicTableEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                count++;
            }

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(opContext, 2);
        }
        #endregion

        #region APM

        [TestMethod]
        [Description("A test to validate basic table query APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryBasicAPM()
        {
            TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

            TableQuerySegment<DynamicTableEntity> seg = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteQuerySegmented(query, null, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                seg = currentTable.EndExecuteQuerySegmented(asyncRes);
            }

            foreach (DynamicTableEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.Properties.Count, 4);
            }
        }

        [TestMethod]
        [Description("A test to validate basic table continuation APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableGenericQueryWithContinuationAPM()
        {
            TableQuery query = new TableQuery();

            OperationContext opContext = new OperationContext();
            TableQuerySegment<DynamicTableEntity> seg = null;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteQuerySegmented(query, null, null, opContext, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                seg = currentTable.EndExecuteQuerySegmented(asyncRes);
            }

            int count = 0;
            foreach (DynamicTableEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                currentTable.BeginExecuteQuerySegmented(query, seg.ContinuationToken, null, opContext, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                seg = currentTable.EndExecuteQuerySegmented<DynamicTableEntity>(asyncRes);
            }

            foreach (DynamicTableEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                count++;
            }

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(opContext, 2);
        }
        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryBasicTask()
        {
            TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));
            query.TakeCount = 100;

            TableQuerySegment<DynamicTableEntity> seg = currentTable.ExecuteQuerySegmentedAsync(query, null).Result;

            foreach (DynamicTableEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.Properties.Count, 4);
            }

            Assert.AreEqual(100, query.TakeCount);
        }

        [TestMethod]
        [Description("A test to validate basic table continuation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithContinuationTask()
        {
            TableQuery query = new TableQuery();

            OperationContext opContext = new OperationContext();
            TableQuerySegment<DynamicTableEntity> seg = currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext).Result;

            int count = 0;
            foreach (DynamicTableEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            seg = currentTable.ExecuteQuerySegmentedAsync(query, seg.ContinuationToken, null, opContext).Result;
            seg.ContinuationToken = null;

            foreach (DynamicTableEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                count++;
            }

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(opContext, 2);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenTask()
        {
            TableQuery query = new TableQuery();
            TableContinuationToken token = null;

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenCancellationTokenTask()
        {
            TableQuery query = new TableQuery();
            TableContinuationToken token = null;
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenTask()
        {
            TableQuery query = new TableQuery();
            EntityResolver<DynamicTableEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                return new DynamicTableEntity(partitionKey, rowKey, timestamp, etag, properties);
            };
            TableContinuationToken token = null;

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenRequestOptionsOperationContextTask()
        {
            TableQuery query = new TableQuery();
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenCancellationTokenTask()
        {
            TableQuery query = new TableQuery();
            EntityResolver<DynamicTableEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                return new DynamicTableEntity(partitionKey, rowKey, timestamp, etag, properties);
            };
            TableContinuationToken token = null;
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryTokenRequestOptionsOperationContextCancellationTokenTask()
        {
            TableQuery query = new TableQuery();
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, token, requestOptions, operationContext, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenRequestOptionsOperationContextTask()
        {
            TableQuery query = new TableQuery();
            EntityResolver<DynamicTableEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                return new DynamicTableEntity(partitionKey, rowKey, timestamp, etag, properties);
            };
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }

        [TestMethod]
        [Description("Test Table ExecuteQuerySegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableExecuteQuerySegmentedQueryResolverTokenRequestOptionsOperationContextCancellationTokenTask()
        {
            TableQuery query = new TableQuery();
            EntityResolver<DynamicTableEntity> resolver = (partitionKey, rowKey, timestamp, properties, etag) =>
            {
                return new DynamicTableEntity(partitionKey, rowKey, timestamp, etag, properties);
            };
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            int count = 0;
            do
            {
                TableQuerySegment<DynamicTableEntity> querySegment = currentTable.ExecuteQuerySegmentedAsync(query, resolver, token, requestOptions, operationContext, cancellationToken).Result;
                token = querySegment.ContinuationToken;

                foreach (DynamicTableEntity entity in querySegment)
                {
                    Assert.IsTrue(entity.PartitionKey.StartsWith("tables_batch"));
                    Assert.AreEqual(entity.Properties.Count, 4);
                    ++count;
                }
            }
            while (token != null);

            Assert.AreEqual(1500, count);
            TestHelper.AssertNAttempts(operationContext, 2);
        }
#endif

        #endregion

        #endregion

        [TestMethod]
        [Description("A test to validate basic table filtering")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithFilter()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryWithFilter(payloadFormat);
            }
        }

        private void DoTableQueryWithFilter(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery().Where(string.Format("(PartitionKey eq '{0}') and (RowKey ge '{1}')", "tables_batch_1", "0050"));

            OperationContext opContext = new OperationContext();
            int count = 0;

            foreach (DynamicTableEntity ent in currentTable.ExecuteQuery(query))
            {
                Assert.AreEqual(ent.Properties["foo"].StringValue, "bar");

                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.RowKey, string.Format("{0:0000}", count + 50));
                count++;
            }

            Assert.AreEqual(count, 50);
        }

        [TestMethod]
        [Description("A test to validate basic table continuation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryEnumerateTwice()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryEnumerateTwice(payloadFormat);
            }
        }

        private void DoTableQueryEnumerateTwice(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery();

            OperationContext opContext = new OperationContext();
            IEnumerable<DynamicTableEntity> enumerable = currentTable.ExecuteQuery(query);

            List<DynamicTableEntity> firstIteration = new List<DynamicTableEntity>();
            List<DynamicTableEntity> secondIteration = new List<DynamicTableEntity>();

            foreach (DynamicTableEntity ent in enumerable)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                firstIteration.Add(ent);
            }

            foreach (DynamicTableEntity ent in enumerable)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                secondIteration.Add(ent);
            }

            Assert.AreEqual(firstIteration.Count, secondIteration.Count);

            for (int m = 0; m < firstIteration.Count; m++)
            {
                Assert.AreEqual(firstIteration[m].PartitionKey, secondIteration[m].PartitionKey);
                Assert.AreEqual(firstIteration[m].RowKey, secondIteration[m].RowKey);
                Assert.AreEqual(firstIteration[m].Properties.Count, secondIteration[m].Properties.Count);
                Assert.AreEqual(firstIteration[m].Timestamp, secondIteration[m].Timestamp);
                Assert.AreEqual(firstIteration[m].ETag, secondIteration[m].ETag);
            }
        }

        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryProjection()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryProjection(payloadFormat, false);
                DoTableQueryProjection(payloadFormat, true);
            }
        }

        private void DoTableQueryProjection(TablePayloadFormat format, bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;

            TableQuery query = new TableQuery().Select(new List<string>() { "a", "c" });

            foreach (DynamicTableEntity ent in currentTable.ExecuteQuery(query))
            {
                Assert.AreEqual(ent.Properties["a"].StringValue, "a");
                Assert.IsFalse(ent.Properties.ContainsKey("b"));
                Assert.AreEqual(ent.Properties["c"].StringValue, "c");
                Assert.IsFalse(ent.Properties.ContainsKey("d"));

                if (tableClient.DefaultRequestOptions.ProjectSystemProperties.HasValue)
                {
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.PartitionKey == default(string), "Missing expected " + TableConstants.PartitionKey);
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.RowKey == default(string), "Missing expected " + TableConstants.RowKey);
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.Timestamp == default(DateTimeOffset), "Missing expected " + TableConstants.Timestamp);
                }
            }
        }

        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryProjectionSpecifyingSystemProperties()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryProjectionSpecifyingSystemProperties(payloadFormat, false);
                DoTableQueryProjectionSpecifyingSystemProperties(payloadFormat, true);
            }
        }

        private void DoTableQueryProjectionSpecifyingSystemProperties(TablePayloadFormat format, bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;

            TableQuery query = new TableQuery().Select(new List<string>() { "a", "c", TableConstants.PartitionKey, TableConstants.Timestamp });

            foreach (DynamicTableEntity ent in currentTable.ExecuteQuery(query))
            {
                Assert.AreEqual(ent.Properties["a"].StringValue, "a");
                Assert.IsFalse(ent.Properties.ContainsKey("b"));
                Assert.AreEqual(ent.Properties["c"].StringValue, "c");
                Assert.IsFalse(ent.Properties.ContainsKey("d"));
                Assert.AreNotEqual(default(string), ent.PartitionKey);
                Assert.AreNotEqual(default(DateTimeOffset), ent.Timestamp);

                if (tableClient.DefaultRequestOptions.ProjectSystemProperties.HasValue)
                {
                    Assert.AreNotEqual(tableClient.DefaultRequestOptions.ProjectSystemProperties.Value, ent.RowKey == default(string), "Missing expected " + TableConstants.RowKey);
                }
            }
        }

        [TestMethod]
        [Description("A test validate all supported query types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryOnSupportedTypes()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryOnSupportedTypes(payloadFormat);
            }
        }

        private void DoTableQueryOnSupportedTypes(TablePayloadFormat format)
        {
            CloudTableClient client = GenerateCloudTableClient();

            CloudTable table = client.GetTableReference(GenerateRandomTableName());
            table.Create();
            client.DefaultRequestOptions.PayloadFormat = format;

            try
            {
                // Setup
                TableBatchOperation batch = new TableBatchOperation();
                string pk = Guid.NewGuid().ToString();
                DynamicTableEntity middleRef = null;
                for (int m = 0; m < 100; m++)
                {
                    ComplexEntity complexEntity = new ComplexEntity();
                    complexEntity.String = string.Format("{0:0000}", m);
                    complexEntity.Binary = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.BinaryPrimitive = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.Bool = m % 2 == 0 ? true : false;
                    complexEntity.BoolPrimitive = m % 2 == 0 ? true : false;
                    complexEntity.Double = m + ((double)m / 100);
                    complexEntity.DoubleInteger = m;
                    complexEntity.DoublePrimitive = m + ((double)m / 100);
                    complexEntity.Int32 = m;
                    complexEntity.IntegerPrimitive = m;
                    complexEntity.Int64 = (long)int.MaxValue + m;
                    complexEntity.LongPrimitive = (long)int.MaxValue + m;
                    complexEntity.Guid = Guid.NewGuid();

                    DynamicTableEntity dynEnt = new DynamicTableEntity(pk, string.Format("{0:0000}", m));
                    dynEnt.Properties = complexEntity.WriteEntity(null);
                    batch.Insert(dynEnt);

                    if (m == 50)
                    {
                        middleRef = dynEnt;
                    }

                    // Add delay to make times unique
                    Thread.Sleep(100);
                }

                table.ExecuteBatch(batch);

                // 1. Filter on String
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterCondition("String", QueryComparisons.GreaterThanOrEqual, "0050"), 50);

                // 2. Filter on Guid
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForGuid("Guid", QueryComparisons.Equal, middleRef["Guid"].GuidValue.Value), 1);

                // 3. Filter on Long
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForLong("Int64", QueryComparisons.GreaterThanOrEqual,
                                middleRef["LongPrimitive"].Int64Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForLong("LongPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["LongPrimitive"].Int64Value.Value), 50);

                // 4. Filter on Double
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Double"].DoubleValue.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForDouble("DoublePrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["DoublePrimitive"].DoubleValue.Value), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", QueryComparisons.GreaterThanOrEqual,
                        middleRef["DoubleInteger"].DoubleValue.Value), 50);

                // 5. Filter on Integer
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForInt("Int32", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Int32"].Int32Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForInt("IntegerPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["IntegerPrimitive"].Int32Value.Value), 50);

                // 6. Filter on Date
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDate("DateTimeOffset", QueryComparisons.GreaterThanOrEqual,
                                middleRef["DateTimeOffset"].DateTimeOffsetValue.Value), 50);

                // 7. Filter on Boolean
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("Bool", QueryComparisons.Equal, middleRef["Bool"].BooleanValue.Value), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("BoolPrimitive", QueryComparisons.Equal, middleRef["BoolPrimitive"].BooleanValue.Value),
                        50);

                // 8. Filter on Binary 
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.Equal, middleRef["Binary"].BinaryValue), 1);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive", QueryComparisons.Equal,
                                middleRef["BinaryPrimitive"].BinaryValue), 1);

                // 9. Filter on Binary GTE
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Binary"].BinaryValue), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["BinaryPrimitive"].BinaryValue), 50);

                // 10. Complex Filter on Binary GTE
                ExecuteQueryAndAssertResults(table, TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                middleRef.PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Binary"].BinaryValue)), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["BinaryPrimitive"].BinaryValue), 50);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("A test validate all supported query types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableRegionalQueryOnSupportedTypes()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableRegionalQueryOnSupportedTypes(payloadFormat);
            }
        }

        private void DoTableRegionalQueryOnSupportedTypes(TablePayloadFormat format)
        {
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            CloudTableClient client = GenerateCloudTableClient();
            CloudTable table = client.GetTableReference(GenerateRandomTableName());
            client.DefaultRequestOptions.PayloadFormat = format;

            try
            {
                table.Create();

                // Setup
                TableBatchOperation batch = new TableBatchOperation();
                string pk = Guid.NewGuid().ToString();
                DynamicTableEntity middleRef = null;
                for (int m = 0; m < 100; m++)
                {
                    ComplexEntity complexEntity = new ComplexEntity();
                    complexEntity.String = string.Format("{0:0000}", m);
                    complexEntity.Binary = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.BinaryPrimitive = new byte[] { 0x01, 0x02, (byte)m };
                    complexEntity.Bool = m % 2 == 0 ? true : false;
                    complexEntity.BoolPrimitive = m % 2 == 0 ? true : false;
                    complexEntity.Double = m + ((double)m / 100);
                    complexEntity.DoublePrimitive = m + ((double)m / 100);
                    complexEntity.Int32 = m;
                    complexEntity.IntegerPrimitive = m;
                    complexEntity.Int64 = (long)int.MaxValue + m;
                    complexEntity.LongPrimitive = (long)int.MaxValue + m;
                    complexEntity.Guid = Guid.NewGuid();

                    DynamicTableEntity dynEnt = new DynamicTableEntity(pk, string.Format("{0:0000}", m));
                    dynEnt.Properties = complexEntity.WriteEntity(null);
                    batch.Insert(dynEnt);

                    if (m == 50)
                    {
                        middleRef = dynEnt;
                    }

                    // Add delay to make times unique
                    Thread.Sleep(100);
                }

                table.ExecuteBatch(batch);

                // 1. Filter on String
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterCondition("String", QueryComparisons.GreaterThanOrEqual, "0050"), 50);

                // 2. Filter on Guid
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForGuid("Guid", QueryComparisons.Equal, middleRef["Guid"].GuidValue.Value), 1);

                // 3. Filter on Long
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForLong("Int64", QueryComparisons.GreaterThanOrEqual,
                                middleRef["LongPrimitive"].Int64Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForLong("LongPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["LongPrimitive"].Int64Value.Value), 50);

                // 4. Filter on Double
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Double"].DoubleValue.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForDouble("DoublePrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["DoublePrimitive"].DoubleValue.Value), 50);

                // 5. Filter on Integer
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForInt("Int32", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Int32"].Int32Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForInt("IntegerPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["IntegerPrimitive"].Int32Value.Value), 50);

                // 6. Filter on Date
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDate("DateTimeOffset", QueryComparisons.GreaterThanOrEqual,
                                middleRef["DateTimeOffset"].DateTimeOffsetValue.Value), 50);

                // 7. Filter on Boolean
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("Bool", QueryComparisons.Equal, middleRef["Bool"].BooleanValue.Value), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("BoolPrimitive", QueryComparisons.Equal, middleRef["BoolPrimitive"].BooleanValue.Value),
                        50);

                // 8. Filter on Binary 
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.Equal, middleRef["Binary"].BinaryValue), 1);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive", QueryComparisons.Equal,
                                middleRef["BinaryPrimitive"].BinaryValue), 1);

                // 9. Filter on Binary GTE
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Binary"].BinaryValue), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["BinaryPrimitive"].BinaryValue), 50);

                // 10. Complex Filter on Binary GTE
                ExecuteQueryAndAssertResults(table, TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                middleRef.PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef["Binary"].BinaryValue)), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef["BinaryPrimitive"].BinaryValue), 50);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                table.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("A test to validate querying with an empty value")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryEmptyValue()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryEmptyValue(payloadFormat);
            }
        }

        private void DoTableQueryEmptyValue(TablePayloadFormat format)
        {
            CloudTableClient client = GenerateCloudTableClient();

            CloudTable table = client.GetTableReference(GenerateRandomTableName());
            table.Create();
            client.DefaultRequestOptions.PayloadFormat = format;

            // Setup
            string pk = Guid.NewGuid().ToString();

            DynamicTableEntity dynEnt = new DynamicTableEntity(pk, "rowkey");
            dynEnt.Properties.Add("A", new EntityProperty(string.Empty));
            table.Execute(TableOperation.Insert(dynEnt));

            // 1. Filter on String
            List<DynamicTableEntity> results = table.ExecuteQuery(new TableQuery().Where(TableQuery.GenerateFilterCondition("A", QueryComparisons.Equal, string.Empty))).ToList();
            Assert.AreEqual(1, results.Count);

            List<BaseEntity> pocoresults = table.ExecuteQuery(new TableQuery<BaseEntity>().Where(TableQuery.GenerateFilterCondition("A", QueryComparisons.Equal, string.Empty))).ToList();
            Assert.AreEqual(1, pocoresults.Count);
        }

        [TestMethod]
        [Description("A test to validate basic take Count with and without continuations")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithTakeCount()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryWithTakeCount(payloadFormat);
            }
        }

        private void DoTableQueryWithTakeCount(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            // No continuation
            TableQuery query = new TableQuery().Take(100);

            OperationContext opContext = new OperationContext();
            IEnumerable<DynamicTableEntity> enumerable = currentTable.ExecuteQuery(query, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 1);

            // With continuations
            query.TakeCount = 1200;
            opContext = new OperationContext();
            enumerable = currentTable.ExecuteQuery(query, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 2);
        }

        [TestMethod]
        [Description("A test to validate basic take Count with a resolver, with and without continuations")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithTakeCountAndResolver()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryWithTakeCountAndResolver(payloadFormat);
            }
        }

        private void DoTableQueryWithTakeCountAndResolver(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // No continuation
            TableQuery query = new TableQuery().Take(100);

            OperationContext opContext = new OperationContext();
            IEnumerable<string> enumerable = currentTable.ExecuteQuery(query, (pk, rk, ts, prop, etag) => pk + rk, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 1);

            // With continuations
            query.TakeCount = 1200;
            opContext = new OperationContext();
            enumerable = currentTable.ExecuteQuery(query, (pk, rk, ts, prop, etag) => pk + rk, null, opContext);

            Assert.AreEqual(query.TakeCount, enumerable.Count());
            TestHelper.AssertNAttempts(opContext, 2);
        }

        #endregion

        #region Negative Tests

        [TestMethod]
        [Description("A test with invalid take count")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithInvalidTakeCount()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryWithInvalidTakeCount(payloadFormat);
            }
        }

        private void DoTableQueryWithInvalidTakeCount(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            try
            {
                TableQuery query = new TableQuery().Take(0);
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.Message, "Take count must be positive and greater than 0.");
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            try
            {
                TableQuery query = new TableQuery().Take(-1);
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.Message, "Take count must be positive and greater than 0.");
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        [Description("A test to invalid query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithInvalidQuery()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryWithInvalidQuery(payloadFormat);
            }
        }

        private void DoTableQueryWithInvalidQuery(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableQuery query = new TableQuery().Where(string.Format("(PartitionKey ) and (RowKey ge '{1}')", "tables_batch_1", "000050"));

            OperationContext opContext = new OperationContext();
            try
            {
                currentTable.ExecuteQuerySegmented(query, null, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "InvalidInput" }, "One of the request inputs is not valid.");
            }
        }

        #endregion

        #region Helpers

        private static void ExecuteQueryAndAssertResults(CloudTable table, string filter, int expectedResults)
        {
            Assert.AreEqual(expectedResults, table.ExecuteQuery(new TableQuery().Where(filter)).Count());
        }

        private static DynamicTableEntity GenerateRandomEntity(string pk)
        {
            DynamicTableEntity ent = new DynamicTableEntity();
            ent.Properties.Add("foo", new EntityProperty("bar"));
            ent.Properties.Add("a", new EntityProperty("a"));
            ent.Properties.Add("b", new EntityProperty("b"));
            ent.Properties.Add("c", new EntityProperty("c"));

            ent.PartitionKey = pk;
            ent.RowKey = Guid.NewGuid().ToString();
            return ent;
        }
        #endregion
    }
}
