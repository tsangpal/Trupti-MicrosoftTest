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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage.Table.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table.Protocol;

#if NETCORE
using System.Globalization;
#else
using Windows.Globalization;
#endif

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableQueryTests : TableTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public TableQueryTests()
        {
            MyClassInitialize(null);
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyClassCleanup();
            MyTestCleanup();
        }
#endif
        readonly CloudTableClient DefaultTableClient = new CloudTableClient(new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint), TestBase.StorageCredentials);

        #region Locals + Ctors

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
            currentTable.CreateIfNotExistsAsync().Wait();

            for (int i = 0; i < 15; i++)
            {
                TableBatchOperation batch = new TableBatchOperation();

                for (int j = 0; j < 100; j++)
                {
                    DynamicTableEntity ent = GenerateRandomEntity("tables_batch_" + i.ToString());
                    ent.RowKey = string.Format("{0:0000}", j);
                    batch.Insert(ent);
                }

                currentTable.ExecuteBatchAsync(batch).Wait();
            }
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            currentTable.DeleteIfExistsAsync().Wait();
        }

        //
        // Use TestInitialize to run code before running each test 
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

        [TestMethod]
        [Description("A test to validate basic table query")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableQueryBasicAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableQueryBasicAsync(payloadFormat);
            }
        }

        private async Task DoTableQueryBasicAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "tables_batch_1"));

            TableQuerySegment seg = await currentTable.ExecuteQuerySegmentedAsync(query, null);

            foreach (DynamicTableEntity ent in seg)
            {
                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.Properties.Count, 4);
            }
        }

        [TestMethod]
        [Description("A test to validate basic table continuation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableQueryWithContinuationAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableQueryWithContinuationAsync(payloadFormat);
            }
        }

        private async Task DoTableQueryWithContinuationAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery();

            OperationContext opContext = new OperationContext();
            TableQuerySegment seg = await currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext);

            int count = 0;
            foreach (DynamicTableEntity ent in seg)
            {
                Assert.IsTrue(ent.PartitionKey.StartsWith("tables_batch"));
                Assert.AreEqual(ent.Properties.Count, 4);
                count++;
            }

            // Second segment
            Assert.IsNotNull(seg.ContinuationToken);
            seg = await currentTable.ExecuteQuerySegmentedAsync(query, seg.ContinuationToken, null, opContext);

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
        [Description("A test to validate basic table filtering")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryWithFilterAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryWithFilterAsync(payloadFormat);
            }
        }

        private void DoTableQueryWithFilterAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery().Where(string.Format("(PartitionKey eq '{0}') and (RowKey ge '{1}')", "tables_batch_1", "0050"));

            OperationContext opContext = new OperationContext();
            int count = 0;

            foreach (DynamicTableEntity ent in ExecuteQuery(currentTable, query))
            {
                Assert.AreEqual(ent.Properties["foo"].StringValue, "bar");

                Assert.AreEqual(ent.PartitionKey, "tables_batch_1");
                Assert.AreEqual(ent.RowKey, string.Format("{0:0000}", count + 50));
                count++;
            }

            Assert.AreEqual(count, 50);
        }

        [TestMethod]
        [Description("Basic projection test")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryProjectionAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryProjectionAsync(payloadFormat, false);
                DoTableQueryProjectionAsync(payloadFormat, true);
            }
        }

        private void DoTableQueryProjectionAsync(TablePayloadFormat format, bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;

            TableQuery query = new TableQuery().Select(new List<string>() { "a", "c" });

            foreach (DynamicTableEntity ent in ExecuteQuery(currentTable, query))
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
        public void TableQueryProjectionAsyncSpecifyingSystemProperties()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryProjectionAsyncSpecifyingSystemProperties(payloadFormat, false);
                DoTableQueryProjectionAsyncSpecifyingSystemProperties(payloadFormat, true);
            }
        }

        private void DoTableQueryProjectionAsyncSpecifyingSystemProperties(TablePayloadFormat format, bool projectSystemProperties)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            tableClient.DefaultRequestOptions.ProjectSystemProperties = projectSystemProperties;

            TableQuery query = new TableQuery().Select(new List<string>() { "a", "c", TableConstants.PartitionKey, TableConstants.Timestamp });

            foreach (DynamicTableEntity ent in ExecuteQuery(currentTable, query))
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
        public async Task TableQueryOnSupportedTypesAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableQueryOnSupportedTypesAsync(payloadFormat);
            }
        }

        private async Task DoTableQueryOnSupportedTypesAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            CloudTable table = tableClient.GetTableReference(GenerateRandomTableName());
            await table.CreateAsync();

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
                    await Task.Delay(100);
                }

                await table.ExecuteBatchAsync(batch);

                // 1. Filter on String
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterCondition("String", QueryComparisons.GreaterThanOrEqual, "0050"), 50);

                // 2. Filter on Guid
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForGuid("Guid", QueryComparisons.Equal, middleRef.Properties["Guid"].GuidValue.Value), 1);

                // 3. Filter on Long
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForLong("Int64", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["LongPrimitive"].Int64Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForLong("LongPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["LongPrimitive"].Int64Value.Value), 50);

                // 4. Filter on Double
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Double"].DoubleValue.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForDouble("DoublePrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["DoublePrimitive"].DoubleValue.Value), 50);

                // 5. Filter on Integer
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForInt("Int32", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Int32"].Int32Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForInt("IntegerPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["IntegerPrimitive"].Int32Value.Value), 50);

                // 6. Filter on Date
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDate("DateTimeOffset", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["DateTimeOffset"].DateTimeOffsetValue.Value), 50);

                // 7. Filter on Boolean
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("Bool", QueryComparisons.Equal, middleRef.Properties["Bool"].BooleanValue.Value), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("BoolPrimitive", QueryComparisons.Equal, middleRef.Properties["BoolPrimitive"].BooleanValue.Value),
                        50);

                // 8. Filter on Binary 
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.Equal, middleRef.Properties["Binary"].BinaryValue), 1);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive", QueryComparisons.Equal,
                                middleRef.Properties["BinaryPrimitive"].BinaryValue), 1);

                // 9. Filter on Binary GTE
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Binary"].BinaryValue), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["BinaryPrimitive"].BinaryValue), 50);

                // 10. Complex Filter on Binary GTE
                ExecuteQueryAndAssertResults(table, TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                middleRef.PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Binary"].BinaryValue)), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["BinaryPrimitive"].BinaryValue), 50);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("A test validate all supported query types")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableRegionalQueryOnSupportedTypesAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableRegionalQueryOnSupportedTypesAsync(payloadFormat);
            }
        }

        private async Task DoTableRegionalQueryOnSupportedTypesAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
#if NETCORE
            //CultureInfo currentCulture = CultureInfo.CurrentCulture;
            //CultureInfo.CurrentCulture = new CultureInfo("tr");
#else
            string currentPrimaryLanguage = ApplicationLanguages.PrimaryLanguageOverride;
            ApplicationLanguages.PrimaryLanguageOverride = "tr";
#endif
            CloudTable table = tableClient.GetTableReference(GenerateRandomTableName());
            await table.CreateAsync();

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
                    await Task.Delay(100);
                }

                await table.ExecuteBatchAsync(batch);

                // 1. Filter on String
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterCondition("String", QueryComparisons.GreaterThanOrEqual, "0050"), 50);

                // 2. Filter on Guid
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForGuid("Guid", QueryComparisons.Equal, middleRef.Properties["Guid"].GuidValue.Value), 1);

                // 3. Filter on Long
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForLong("Int64", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["LongPrimitive"].Int64Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForLong("LongPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["LongPrimitive"].Int64Value.Value), 50);

                // 4. Filter on Double
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDouble("Double", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Double"].DoubleValue.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForDouble("DoublePrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["DoublePrimitive"].DoubleValue.Value), 50);

                // 5. Filter on Integer
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForInt("Int32", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Int32"].Int32Value.Value), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForInt("IntegerPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["IntegerPrimitive"].Int32Value.Value), 50);

                // 6. Filter on Date
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForDate("DateTimeOffset", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["DateTimeOffset"].DateTimeOffsetValue.Value), 50);

                // 7. Filter on Boolean
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("Bool", QueryComparisons.Equal, middleRef.Properties["Bool"].BooleanValue.Value), 50);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBool("BoolPrimitive", QueryComparisons.Equal, middleRef.Properties["BoolPrimitive"].BooleanValue.Value),
                        50);

                // 8. Filter on Binary 
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.Equal, middleRef.Properties["Binary"].BinaryValue), 1);

                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive", QueryComparisons.Equal,
                                middleRef.Properties["BinaryPrimitive"].BinaryValue), 1);

                // 9. Filter on Binary GTE
                ExecuteQueryAndAssertResults(table,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Binary"].BinaryValue), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["BinaryPrimitive"].BinaryValue), 50);

                // 10. Complex Filter on Binary GTE
                ExecuteQueryAndAssertResults(table, TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                middleRef.PartitionKey),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForBinary("Binary", QueryComparisons.GreaterThanOrEqual,
                                middleRef.Properties["Binary"].BinaryValue)), 50);

                ExecuteQueryAndAssertResults(table, TableQuery.GenerateFilterConditionForBinary("BinaryPrimitive",
                        QueryComparisons.GreaterThanOrEqual, middleRef.Properties["BinaryPrimitive"].BinaryValue), 50);
            }
            finally
            {
#if NETCORE
                //CultureInfo.CurrentCulture = currentCulture;
#else
                ApplicationLanguages.PrimaryLanguageOverride = currentPrimaryLanguage;
#endif
                table.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("A test to validate querying with an empty value")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableQueryEmptyValueAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableQueryEmptyValueAsync(payloadFormat);
            }
        }

        private async Task DoTableQueryEmptyValueAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            CloudTable table = tableClient.GetTableReference(GenerateRandomTableName());
            await table.CreateAsync();

            // Setup
            string pk = Guid.NewGuid().ToString();

            DynamicTableEntity dynEnt = new DynamicTableEntity(pk, "rowkey");
            dynEnt.Properties.Add("A", new EntityProperty(string.Empty));
            await table.ExecuteAsync(TableOperation.Insert(dynEnt));

            // 1. Filter on String
            List<DynamicTableEntity> results = (await table.ExecuteQuerySegmentedAsync(new TableQuery().Where(TableQuery.GenerateFilterCondition("A", QueryComparisons.Equal, string.Empty)), null)).ToList();
            Assert.AreEqual(1, results.Count);

            List<BaseEntity> pocoresults = (await table.ExecuteQuerySegmentedAsync(new TableQuery<BaseEntity>().Where(TableQuery.GenerateFilterCondition("A", QueryComparisons.Equal, string.Empty)), null)).ToList();
            Assert.AreEqual(1, pocoresults.Count);
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
        public async Task TableQueryWithInvalidQueryAsync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                await DoTableQueryWithInvalidQueryAsync(payloadFormat);
            }
        }

        private async Task DoTableQueryWithInvalidQueryAsync(TablePayloadFormat format)
        {
            tableClient.DefaultRequestOptions.PayloadFormat = format;
            TableQuery query = new TableQuery().Where(string.Format("(PartitionKey ) and (RowKey ge '{1}')", "tables_batch_1", "000050"));

            OperationContext opContext = new OperationContext();
            try
            {
                await currentTable.ExecuteQuerySegmentedAsync(query, null, null, opContext);
                Assert.Fail();
            }
            catch (Exception)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "InvalidInput" }, "One of the request inputs is not valid.");
            }
        }

#endregion

#region Helpers

        private static List<DynamicTableEntity> ExecuteQuery(CloudTable table, TableQuery query)
        {
            List<DynamicTableEntity> retList = new List<DynamicTableEntity>();

            TableQuerySegment currSeg = null;

            while (currSeg == null || currSeg.ContinuationToken != null)
            {
                Task<TableQuerySegment> task = Task.Run(() => table.ExecuteQuerySegmentedAsync(query, currSeg != null ? currSeg.ContinuationToken : null));
                task.Wait();
                currSeg = task.Result;
                retList.AddRange(currSeg.Results);
            }

            return retList;
        }

        private static void ExecuteQueryAndAssertResults(CloudTable table, string filter, int expectedResults)
        {
            Assert.AreEqual(expectedResults, ExecuteQuery(table, new TableQuery().Where(filter)).Count());
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
