// -----------------------------------------------------------------------------------------
// <copyright file="TableSasUnitTests.cs" company="Microsoft">
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
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class TableSasUnitTests : TableTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public TableSasUnitTests()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif
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

        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }

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

        #region Constructor Tests

        [TestMethod]
        [Description("Test TableSas via various constructors Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSASConstructorsAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));
            try
            {
                await table.CreateAsync();

                await table.ExecuteAsync(TableOperation.Insert(new BaseEntity("PK", "RK")));

                // Prepare SAS authentication with full permissions
                string sasToken = table.GetSharedAccessSignature(
                    new SharedAccessTablePolicy
                    {
                        Permissions = SharedAccessTablePermissions.Add | SharedAccessTablePermissions.Delete | SharedAccessTablePermissions.Query,
                        SharedAccessExpiryTime = DateTimeOffset.Now.AddMinutes(30)
                    },
                    null /* accessPolicyIdentifier */,
                    null /* startPk */,
                    null /* startRk */,
                    null /* endPk */,
                    null /* endRk */);

                CloudStorageAccount sasAccount;
                StorageCredentials sasCreds;
                CloudTableClient sasClient;
                CloudTable sasTable;
                Uri baseUri = new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint);

                // SAS via connection string parse
                sasAccount = CloudStorageAccount.Parse(string.Format("TableEndpoint={0};SharedAccessSignature={1}", baseUri.AbsoluteUri, sasToken));
                sasClient = sasAccount.CreateCloudTableClient();
                sasTable = sasClient.GetTableReference(table.Name);

                Assert.AreEqual(1, (await sasTable.ExecuteQuerySegmentedAsync(new TableQuery<BaseEntity>(), null)).Results.Count());

                // SAS via account constructor
                sasCreds = new StorageCredentials(sasToken);
                sasAccount = new CloudStorageAccount(sasCreds, null, null, baseUri, null);
                sasClient = sasAccount.CreateCloudTableClient();
                sasTable = sasClient.GetTableReference(table.Name);
                Assert.AreEqual(1, (await sasTable.ExecuteQuerySegmentedAsync(new TableQuery<BaseEntity>(), null)).Results.Count());

                // SAS via client constructor URI + Creds
                sasCreds = new StorageCredentials(sasToken);
                sasClient = new CloudTableClient(baseUri, sasCreds);
                Assert.AreEqual(1, (await sasTable.ExecuteQuerySegmentedAsync(new TableQuery<BaseEntity>(), null)).Results.Count());

                // SAS via CloudTable constructor Uri + Client
                sasCreds = new StorageCredentials(sasToken);
                sasTable = new CloudTable(table.Uri, tableClient.Credentials);
                sasClient = sasTable.ServiceClient;
                Assert.AreEqual(1, (await sasTable.ExecuteQuerySegmentedAsync(new TableQuery<BaseEntity>(), null)).Results.Count());
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        #endregion

        #region Permissions

        [TestMethod]
        [Description("Tests setting and getting table permissions Async")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSetGetPermissionsAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                await table.CreateAsync();

                await table.ExecuteAsync(TableOperation.Insert(new BaseEntity("PK", "RK")));

                TablePermissions expectedPermissions = new TablePermissions();
                TablePermissions testPermissions = await table.GetPermissionsAsync();

                AssertPermissionsEqual(expectedPermissions, testPermissions);

                // Add a policy, check setting and getting.
                expectedPermissions.SharedAccessPolicies.Add(Guid.NewGuid().ToString(), new SharedAccessTablePolicy
                {
                    Permissions = SharedAccessTablePermissions.Query,
                    SharedAccessStartTime = DateTimeOffset.Now - TimeSpan.FromHours(1),
                    SharedAccessExpiryTime = DateTimeOffset.Now + TimeSpan.FromHours(1)
                });

                await table.SetPermissionsAsync(expectedPermissions);
                await Task.Delay(30 * 1000);
                testPermissions = await table.GetPermissionsAsync();
                AssertPermissionsEqual(expectedPermissions, testPermissions);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Tests Null Access Policy")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSASNullAccessPolicyAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                await table.CreateAsync();

                await table.ExecuteAsync(TableOperation.Insert(new BaseEntity("PK", "RK")));

                TablePermissions expectedPermissions = new TablePermissions();

                // Add a policy
                expectedPermissions.SharedAccessPolicies.Add(Guid.NewGuid().ToString(), new SharedAccessTablePolicy
                {
                    Permissions = SharedAccessTablePermissions.Query | SharedAccessTablePermissions.Add,
                    SharedAccessStartTime = DateTimeOffset.Now - TimeSpan.FromHours(1),
                    SharedAccessExpiryTime = DateTimeOffset.Now + TimeSpan.FromHours(1)
                });

                await table.SetPermissionsAsync(expectedPermissions);
                await Task.Delay(30 * 1000);

                // Generate the sasToken the user should use
                string sasToken = table.GetSharedAccessSignature(null, expectedPermissions.SharedAccessPolicies.First().Key, "AAAA", null, "AAAA", null);

                CloudTable sasTable = new CloudTable(table.Uri, new StorageCredentials(sasToken));

                await sasTable.ExecuteAsync(TableOperation.Insert(new DynamicTableEntity("AAAA", "foo")));

                TableResult result = await sasTable.ExecuteAsync(TableOperation.Retrieve("AAAA", "foo"));

                Assert.IsNotNull(result.Result);

                // revoke table permissions
                await table.SetPermissionsAsync(new TablePermissions());
                await Task.Delay(30 * 1000);

                OperationContext opContext = new OperationContext();
                try
                {
                    await sasTable.ExecuteAsync(TableOperation.Insert(new DynamicTableEntity("AAAA", "foo2")), null, opContext);
                    Assert.Fail();
                }
                catch (Exception)
                {
                    Assert.AreEqual(opContext.LastResult.HttpStatusCode, (int)HttpStatusCode.Forbidden);
                }

                opContext = new OperationContext();
                try
                {
                    result = await sasTable.ExecuteAsync(TableOperation.Retrieve("AAAA", "foo"), null, opContext);
                    Assert.Fail();
                }
                catch (Exception)
                {
                    Assert.AreEqual(opContext.LastResult.HttpStatusCode, (int)HttpStatusCode.Forbidden);
                }
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        #endregion

        #region SAS Operations

        [TestMethod]
        [Description("Tests table SAS with query permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSasQueryTestAsync()
        {
            await TestTableSas(SharedAccessTablePermissions.Query);
        }

        [TestMethod]
        [Description("Tests table SAS with delete permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSasDeleteTestAsync()
        {
            await TestTableSas(SharedAccessTablePermissions.Delete);
        }

        [TestMethod]
        [Description("Tests table SAS with process and update permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSasUpdateTestAsync()
        {
            await TestTableSas(SharedAccessTablePermissions.Update);
        }

        [TestMethod]
        [Description("Tests table SAS with add permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSasAddTestAsync()
        {
            await TestTableSas(SharedAccessTablePermissions.Add);
        }

        [TestMethod]
        [Description("Tests table SAS with full permissions.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSasFullTestAsync()
        {
            await TestTableSas(SharedAccessTablePermissions.Query | SharedAccessTablePermissions.Delete | SharedAccessTablePermissions.Update | SharedAccessTablePermissions.Add);
        }

        /// <summary>
        /// Tests table access permissions with SAS, using a stored policy and using permissions on the URI.
        /// Various table range constraints are tested.
        /// </summary>
        /// <param name="accessPermissions">The permissions to test.</param>
        internal async Task TestTableSas(SharedAccessTablePermissions accessPermissions)
        {
            string startPk = "M";
            string startRk = "F";
            string endPk = "S";
            string endRk = "T";

            // No ranges specified
            await TestTableSasWithRange(accessPermissions, null, null, null, null);

            // All ranges specified
            await TestTableSasWithRange(accessPermissions, startPk, startRk, endPk, endRk);

            // StartPk & StartRK specified
            await TestTableSasWithRange(accessPermissions, startPk, startRk, null, null);

            // StartPk specified
            await TestTableSasWithRange(accessPermissions, startPk, null, null, null);

            // EndPk & EndRK specified
            await TestTableSasWithRange(accessPermissions, null, null, endPk, endRk);

            // EndPk specified
            await TestTableSasWithRange(accessPermissions, null, null, endPk, null);

            // StartPk and EndPk specified
            await TestTableSasWithRange(accessPermissions, startPk, null, endPk, null);

            // StartRk and StartRK and EndPk specified
            await TestTableSasWithRange(accessPermissions, startPk, startRk, endPk, null);

            // StartRk and EndPK and EndPk specified
            await TestTableSasWithRange(accessPermissions, startPk, null, endPk, endRk);
        }

        /// <summary>
        /// Tests table access permissions with SAS, using a stored policy and using permissions on the URI.
        /// </summary>
        /// <param name="accessPermissions">The permissions to test.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        internal async Task TestTableSasWithRange(
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                await table.CreateAsync();

                // Set up a policy
                string identifier = Guid.NewGuid().ToString();
                TablePermissions permissions = new TablePermissions();
                permissions.SharedAccessPolicies.Add(identifier, new SharedAccessTablePolicy
                {
                    Permissions = accessPermissions,
                    SharedAccessExpiryTime = DateTimeOffset.Now.AddDays(1)
                });

                await table.SetPermissionsAsync(permissions);
                await Task.Delay(30 * 1000);

                // Prepare SAS authentication using access identifier
                string sasString = table.GetSharedAccessSignature(new SharedAccessTablePolicy(), identifier, startPk, startRk, endPk, endRk);
                CloudTableClient identifierSasClient = new CloudTableClient(tableClient.BaseUri, new StorageCredentials(sasString));

                // Prepare SAS authentication using explicit policy
                sasString = table.GetSharedAccessSignature(
                                        new SharedAccessTablePolicy
                                        {
                                            Permissions = accessPermissions,
                                            SharedAccessExpiryTime = DateTimeOffset.Now.AddMinutes(30)
                                        },
                                        null,
                                        startPk,
                                        startRk,
                                        endPk,
                                        endRk);

                CloudTableClient explicitSasClient = new CloudTableClient(tableClient.BaseUri, new StorageCredentials(sasString));

                // Point query
                await TestPointQuery(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                await TestPointQuery(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Add row
                await TestAdd(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                await TestAdd(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Update row (merge)
                await TestUpdateMerge(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                await TestUpdateMerge(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Update row (replace)
                await TestUpdateReplace(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                await TestUpdateReplace(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Delete row
                await TestDelete(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                await TestDelete(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Upsert row (merge)
                await TestUpsertMerge(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                await TestUpsertMerge(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);

                // Upsert row (replace)
                await TestUpsertReplace(identifierSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
                await TestUpsertReplace(explicitSasClient, table.Name, accessPermissions, startPk, startRk, endPk, endRk);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }


        /// <summary>
        /// Test point queries entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private async Task TestPointQuery(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Query) != 0;

            Action<BaseEntity, OperationContext> queryDelegate = (tableEntity, ctx) =>
            {
                Task<TableResult> retrieveTask = testClient.GetTableReference(tableName).ExecuteAsync(TableOperation.Retrieve<BaseEntity>(tableEntity.PartitionKey, tableEntity.RowKey), null, ctx);
                retrieveTask.Wait();

                if (expectSuccess)
                {
                    Assert.IsNotNull(retrieveTask.Result.Result);
                }
                else
                {
                    Assert.AreEqual(ctx.LastResult.HttpStatusCode, (int)HttpStatusCode.OK);
                }
            };


            // Perform test
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                queryDelegate,
                "point query",
                expectSuccess,
                expectSuccess ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                false,
                expectSuccess);
        }


        /// <summary>
        /// Test update (merge) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private async Task TestUpdateMerge(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> updateDelegate = (tableEntity, ctx) =>
            {
                // Merge entity
                tableEntity.A = "10";
                tableEntity.ETag = "*";
                testClient.GetTableReference(tableName).ExecuteAsync(TableOperation.Merge(tableEntity), null, ctx).Wait();
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Update) != 0;

            // Perform test
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                updateDelegate,
                "update merge",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden,
                false,
                false);
        }

        /// <summary>
        /// Test update (replace) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private async Task TestUpdateReplace(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> updateDelegate = (tableEntity, ctx) =>
            {
                // replace entity
                tableEntity.A = "20";
                tableEntity.ETag = "*";
                testClient.GetTableReference(tableName).ExecuteAsync(TableOperation.Replace(tableEntity), null, ctx).Wait();
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Update) != 0;

            // Perform test
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                updateDelegate,
                "update replace",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden,
                false,
                false);
        }

        /// <summary>
        /// Test adding entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private async Task TestAdd(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> addDelegate = (tableEntity, ctx) =>
            {
                // insert entity
                tableEntity.A = "10";
                testClient.GetTableReference(tableName).ExecuteAsync(TableOperation.Insert(tableEntity), null, ctx).Wait();
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Add) != 0;

            // Perform test
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                addDelegate,
                "add",
                expectSuccess,
                expectSuccess ? HttpStatusCode.Created : HttpStatusCode.Forbidden,
                false,
                false);
        }

        /// <summary>
        /// Test deleting entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private async Task TestDelete(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> deleteDelegate = (tableEntity, ctx) =>
            {
                // delete entity
                tableEntity.A = "10";
                tableEntity.ETag = "*";
                testClient.GetTableReference(tableName).ExecuteAsync(TableOperation.Delete(tableEntity), null, ctx).Wait();
            };

            bool expectSuccess = (accessPermissions & SharedAccessTablePermissions.Delete) != 0;

            // Perform test
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                deleteDelegate,
                "delete",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden,
                false,
                false);
        }

        /// <summary>
        /// Test upsert (insert or merge) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private async Task TestUpsertMerge(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> upsertDelegate = (tableEntity, ctx) =>
            {
                // insert or merge entity
                tableEntity.A = "10";
                testClient.GetTableReference(tableName).ExecuteAsync(TableOperation.InsertOrMerge(tableEntity), null, ctx).Wait();
            };

            SharedAccessTablePermissions upsertPermissions = (SharedAccessTablePermissions.Update | SharedAccessTablePermissions.Add);
            bool expectSuccess = (accessPermissions & upsertPermissions) == upsertPermissions;

            // Perform test
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                upsertDelegate,
                "upsert merge",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden,
                false,
                false);
        }

        /// <summary>
        /// Test upsert (insert or replace) on entities inside and outside the given range.
        /// </summary>
        /// <param name="testClient">The table client to test.</param>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="accessPermissions">The access permissions of the table client.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        private async Task TestUpsertReplace(
            CloudTableClient testClient,
            string tableName,
            SharedAccessTablePermissions accessPermissions,
            string startPk,
            string startRk,
            string endPk,
            string endRk)
        {
            Action<BaseEntity, OperationContext> upsertDelegate = (tableEntity, ctx) =>
            {
                // insert or replace entity
                tableEntity.A = "10";
                testClient.GetTableReference(tableName).ExecuteAsync(TableOperation.InsertOrReplace(tableEntity), null, ctx).Wait();
            };

            SharedAccessTablePermissions upsertPermissions = (SharedAccessTablePermissions.Update | SharedAccessTablePermissions.Add);
            bool expectSuccess = (accessPermissions & upsertPermissions) == upsertPermissions;

            // Perform test
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                upsertDelegate,
                "upsert replace",
                expectSuccess,
                expectSuccess ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden,
                false,
                false);
        }

        /// <summary>
        /// Test a table operation on entities inside and outside the given range.
        /// </summary>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        /// <param name="runOperationDelegate">A delegate with the table operation to test.</param>
        /// <param name="opName">The name of the operation being tested.</param>
        /// <param name="expectSuccess">Whether the operation should succeed on entities within the range.</param>
        /// <param name="expectedStatusCode">The status code expected for the response.</param>
        /// <param name="isRangeQuery">Specifies if the operation is a range query.</param>
        private async Task TestOperationWithRange(
            string tableName,
            string startPk,
            string startRk,
            string endPk,
            string endRk,
            Action<BaseEntity, OperationContext> runOperationDelegate,
            string opName,
            bool expectSuccess,
            HttpStatusCode expectedStatusCode,
            bool isRangeQuery)
        {
            await TestOperationWithRange(
                tableName,
                startPk,
                startRk,
                endPk,
                endRk,
                runOperationDelegate,
                opName,
                expectSuccess,
                expectedStatusCode,
                isRangeQuery,
                false /* isPointQuery */);
        }

        /// <summary>
        /// Test a table operation on entities inside and outside the given range.
        /// </summary>
        /// <param name="tableName">The name of the table to test.</param>
        /// <param name="startPk">The start partition key range.</param>
        /// <param name="startRk">The start row key range.</param>
        /// <param name="endPk">The end partition key range.</param>
        /// <param name="endRk">The end row key range.</param>
        /// <param name="runOperationDelegate">A delegate with the table operation to test.</param>
        /// <param name="opName">The name of the operation being tested.</param>
        /// <param name="expectSuccess">Whether the operation should succeed on entities within the range.</param>
        /// <param name="expectedStatusCode">The status code expected for the response.</param>
        /// <param name="isRangeQuery">Specifies if the operation is a range query.</param>
        /// <param name="isPointQuery">Specifies if the operation is a point query.</param>
        private async Task TestOperationWithRange(
            string tableName,
            string startPk,
            string startRk,
            string endPk,
            string endRk,
            Action<BaseEntity, OperationContext> runOperationDelegate,
            string opName,
            bool expectSuccess,
            HttpStatusCode expectedStatusCode,
            bool isRangeQuery,
            bool isPointQuery)
        {
            CloudTableClient referenceClient = GenerateCloudTableClient();

            string partitionKey = startPk ?? endPk ?? "M";
            string rowKey = startRk ?? endRk ?? "S";

            // if we expect a success for creation - avoid inserting duplicate entities
            BaseEntity tableEntity = new BaseEntity(partitionKey, rowKey);
            if (expectedStatusCode == HttpStatusCode.Created)
            {
                try
                {
                    tableEntity.ETag = "*";
                    await referenceClient.GetTableReference(tableName).ExecuteAsync(TableOperation.Delete(tableEntity));
                }
                catch (Exception)
                {
                }
            }
            else
            {
                // only for add we should not be adding the entity
                await referenceClient.GetTableReference(tableName).ExecuteAsync(TableOperation.InsertOrReplace(tableEntity));
            }

            if (expectSuccess)
            {
                runOperationDelegate(tableEntity, null);
            }
            else
            {
                TestHelper.ExpectedException(
                    (ctx) => runOperationDelegate(tableEntity, ctx),
                    string.Format("{0} without appropriate permission.", opName),
                    (int)HttpStatusCode.Forbidden);
            }

            if (startPk != null)
            {
                tableEntity.PartitionKey = "A";
                if (startPk.CompareTo(tableEntity.PartitionKey) <= 0)
                {
                    Assert.Inconclusive("Test error: partition key for this test must not be less than or equal to \"A\"");
                }

                TestHelper.ExpectedException(
                    (ctx) => runOperationDelegate(tableEntity, ctx),
                    string.Format("{0} before allowed partition key range", opName),
                    (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));
                tableEntity.PartitionKey = partitionKey;
            }

            if (endPk != null)
            {
                tableEntity.PartitionKey = "Z";
                if (endPk.CompareTo(tableEntity.PartitionKey) >= 0)
                {
                    Assert.Inconclusive("Test error: partition key for this test must not be greater than or equal to \"Z\"");
                }

                TestHelper.ExpectedException(
                    (ctx) => runOperationDelegate(tableEntity, ctx),
                    string.Format("{0} after allowed partition key range", opName),
                    (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));

                tableEntity.PartitionKey = partitionKey;
            }

            if (startRk != null)
            {
                if (isRangeQuery || startPk != null)
                {
                    tableEntity.PartitionKey = startPk;
                    tableEntity.RowKey = "A";
                    if (startRk.CompareTo(tableEntity.RowKey) <= 0)
                    {
                        Assert.Inconclusive("Test error: row key for this test must not be less than or equal to \"A\"");
                    }

                    TestHelper.ExpectedException(
                        (ctx) => runOperationDelegate(tableEntity, ctx),
                        string.Format("{0} before allowed row key range", opName),
                        (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));

                    tableEntity.RowKey = rowKey;
                }
            }

            if (endRk != null)
            {
                if (isRangeQuery || endPk != null)
                {
                    tableEntity.PartitionKey = endPk;
                    tableEntity.RowKey = "Z";
                    if (endRk.CompareTo(tableEntity.RowKey) >= 0)
                    {
                        Assert.Inconclusive("Test error: row key for this test must not be greater than or equal to \"Z\"");
                    }

                    TestHelper.ExpectedException(
                        (ctx) => runOperationDelegate(tableEntity, ctx),
                        string.Format("{0} after allowed row key range", opName),
                        (int)(isPointQuery ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));

                    tableEntity.RowKey = rowKey;
                }
            }
        }
        #endregion

        #region SAS Error Conditions

        //[TestMethod] // Disabled until service bug is fixed
        [Description("Attempt to use SAS to authenticate table operations that must not work with SAS.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSasInvalidOperations()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                await table.CreateAsync();
                // Prepare SAS authentication with full permissions
                string sasString = table.GetSharedAccessSignature(
                                        new SharedAccessTablePolicy
                                        {
                                            Permissions = SharedAccessTablePermissions.Delete,
                                            SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30)
                                        },
                                        null,
                                        null,
                                        null,
                                        null,
                                        null);

                CloudTableClient sasClient = new CloudTableClient(tableClient.BaseUri, new StorageCredentials(sasString));

                // Construct a valid set of service properties to upload.
                ServiceProperties properties = new ServiceProperties(new LoggingProperties(), new MetricsProperties(), new MetricsProperties());
                properties.Logging.Version = "1.0";
                properties.HourMetrics.Version = "1.0";
                properties.Logging.RetentionDays = 9;
                await sasClient.GetServicePropertiesAsync();
                await sasClient.SetServicePropertiesAsync(properties);

                // Test invalid client operations
                // BUGBUG: ListTables hides the exception. We should fix this
                // TestHelpers.ExpectedException(() => sasClient.ListTablesSegmented(), "List tables with SAS", HttpStatusCode.NotFound);
                TestHelper.ExpectedException((ctx) => sasClient.GetServicePropertiesAsync().Wait(), "Get service properties with SAS", (int)HttpStatusCode.NotFound);
                TestHelper.ExpectedException((ctx) => sasClient.SetServicePropertiesAsync(properties).Wait(), "Set service properties with SAS", (int)HttpStatusCode.NotFound);

                CloudTable sasTable = sasClient.GetTableReference(table.Name);

                // Verify that creation fails with SAS
                TestHelper.ExpectedException((ctx) => sasTable.CreateAsync(null, ctx).Wait(), "Create a table with SAS", (int)HttpStatusCode.NotFound);

                // Create the table.
                await table.CreateAsync();

                // Test invalid table operations
                TestHelper.ExpectedException((ctx) => sasTable.DeleteAsync(null, ctx).Wait(), "Delete a table with SAS", (int)HttpStatusCode.NotFound);
                TestHelper.ExpectedException((ctx) => sasTable.GetPermissionsAsync(null, ctx).Wait(), "Get ACL with SAS", (int)HttpStatusCode.NotFound);
                TestHelper.ExpectedException((ctx) => sasTable.SetPermissionsAsync(new TablePermissions(), null, ctx).Wait(), "Set ACL with SAS", (int)HttpStatusCode.NotFound);
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }

        #endregion

        #region Update SAS token
        [TestMethod]
        [Description("Update table SAS.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableUpdateSasTestAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                await table.CreateAsync();

                BaseEntity entity = new BaseEntity("PK", "RK");
                await table.ExecuteAsync(TableOperation.Insert(entity));

                SharedAccessTablePolicy policy = new SharedAccessTablePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessTablePermissions.Delete,
                };

                string sasToken = table.GetSharedAccessSignature(policy, null, null, null, null, null);
                StorageCredentials creds = new StorageCredentials(sasToken);
                CloudTable sasTable = new CloudTable(table.Uri, creds);
                OperationContext context = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await sasTable.ExecuteAsync(TableOperation.Insert(new BaseEntity("PK", "RK2")), null, context),
                    context,
                    "Try to insert an entity when SAS doesn't allow inserts",
                    HttpStatusCode.Forbidden);

                await sasTable.ExecuteAsync(TableOperation.Delete(entity));

                SharedAccessTablePolicy policy2 = new SharedAccessTablePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessTablePermissions.Delete | SharedAccessTablePermissions.Add,
                };

                string sasToken2 = table.GetSharedAccessSignature(policy2, null, null, null, null, null);
                creds.UpdateSASToken(sasToken2);

                sasTable = new CloudTable(table.Uri, creds);

                await sasTable.ExecuteAsync(TableOperation.Insert(new BaseEntity("PK", "RK2")));

            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }
        #endregion

        #region SasUri
        [TestMethod]
        [Description("Use table SasUri.")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task TableSasUriTestAsync()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("T" + Guid.NewGuid().ToString("N"));

            try
            {
                await table.CreateAsync();

                BaseEntity entity = new BaseEntity("PK", "RK");
                BaseEntity entity1 = new BaseEntity("PK", "RK1");
                await table.ExecuteAsync(TableOperation.Insert(entity));
                await table.ExecuteAsync(TableOperation.Insert(entity1));

                SharedAccessTablePolicy policy = new SharedAccessTablePolicy()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(30),
                    Permissions = SharedAccessTablePermissions.Delete,
                };

                string sasToken = table.GetSharedAccessSignature(policy, null, null, null, null, null);
                StorageCredentials creds = new StorageCredentials(sasToken);
                CloudStorageAccount sasAcc = new CloudStorageAccount(creds, null /* blobEndpoint */, null /* queueEndpoint */, new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint), null /* fileEndpoint */);
                CloudTableClient client = sasAcc.CreateCloudTableClient();

                CloudTable sasTable = new CloudTable(client.Credentials.TransformUri(table.Uri));
                await sasTable.ExecuteAsync(TableOperation.Delete(entity));

                CloudTable sasTable2 = new CloudTable(new Uri(table.Uri.ToString() + sasToken));
                await sasTable2.ExecuteAsync(TableOperation.Delete(entity1));
            }
            finally
            {
                table.DeleteIfExistsAsync().Wait();
            }
        }
        #endregion

        #region Test Helpers
        internal static void AssertPermissionsEqual(TablePermissions permissions1, TablePermissions permissions2)
        {
            Assert.AreEqual(permissions1.SharedAccessPolicies.Count, permissions2.SharedAccessPolicies.Count);

            foreach (KeyValuePair<string, SharedAccessTablePolicy> pair in permissions1.SharedAccessPolicies)
            {
                SharedAccessTablePolicy policy1 = pair.Value;
                SharedAccessTablePolicy policy2 = permissions2.SharedAccessPolicies[pair.Key];

                Assert.IsNotNull(policy1);
                Assert.IsNotNull(policy2);

                Assert.AreEqual(policy1.Permissions, policy2.Permissions);
                if (policy1.SharedAccessStartTime != null)
                {
                    Assert.IsTrue(Math.Floor((policy1.SharedAccessStartTime.Value - policy2.SharedAccessStartTime.Value).TotalSeconds) == 0);
                }

                if (policy1.SharedAccessExpiryTime != null)
                {
                    Assert.IsTrue(Math.Floor((policy1.SharedAccessExpiryTime.Value - policy2.SharedAccessExpiryTime.Value).TotalSeconds) == 0);
                }
            }
        }
        #endregion
    }
}
