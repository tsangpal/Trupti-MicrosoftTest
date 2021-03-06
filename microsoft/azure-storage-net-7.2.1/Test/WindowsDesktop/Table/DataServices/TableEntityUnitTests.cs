// -----------------------------------------------------------------------------------------
// <copyright file="TableEntityUnitTests.cs" company="Microsoft">
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

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table.DataServices.Entities;

namespace Microsoft.WindowsAzure.Storage.Table.DataServices
{
    [TestClass]
    [Obsolete("Support for accessing Windows Azure Tables via WCF Data Services is now obsolete. It's recommended that you use the Microsoft.WindowsAzure.Storage.Table namespace for working with tables.")]
    public class TableEntityUnitTests : TableTestBase
    {
        #region Locals + Ctors
        public TableEntityUnitTests()
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

        CloudTable currentTable = null;

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
            CloudTableClient tableClient = GenerateCloudTableClient();
            currentTable = tableClient.GetTableReference(GenerateRandomTableName());
            currentTable.CreateIfNotExists();
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            currentTable.DeleteIfExists();
        }

        #endregion

        #region Insert

        [TestMethod]
        [Description("Single Entity Insert")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsert()
        {
            DoSingleEntityInsert(TablePayloadFormat.Json);
        }

        private void DoSingleEntityInsert(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);

            // Insert Entity
            ComplexEntity insertEntity = new ComplexEntity("insert test", "foo" + format.ToString());
            ctx.AddObject(currentTable.Name, insertEntity);
            ctx.SaveChangesWithRetries();

            // Retrieve Entity
            ComplexEntity retrievedEntity = (from ent in ctx.CreateQuery<ComplexEntity>(currentTable.Name)
                                             where ent.PartitionKey == insertEntity.PartitionKey
                                             && ent.RowKey == insertEntity.RowKey
                                             select ent).AsTableServiceQuery(ctx).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            ComplexEntity.AssertEquality(insertEntity, retrievedEntity);
        }

#if TASK
        [TestMethod]
        [Description("Single Entity Insert")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();

            // Insert Entity
            ComplexEntity insertEntity = new ComplexEntity("insert test", "foo");
            ctx.AddObject(currentTable.Name, insertEntity);
            ctx.SaveChangesWithRetriesAsync().Wait();

            // Retrieve Entity
            ComplexEntity retrievedEntity = (from ent in ctx.CreateQuery<ComplexEntity>(currentTable.Name)
                                             where ent.PartitionKey == insertEntity.PartitionKey
                                             && ent.RowKey == insertEntity.RowKey
                                             select ent).AsTableServiceQuery(ctx).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            ComplexEntity.AssertEquality(insertEntity, retrievedEntity);
        }
#endif

        [TestMethod]
        [Description("Single Entity Insert Conflict")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertConflict()
        {
            DoSingleEntityInsertConflict(TablePayloadFormat.Json);
        }

        private void DoSingleEntityInsertConflict(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);

            // Insert Entity
            ComplexEntity insertEntity = new ComplexEntity("insert test", "foo" + format.ToString());
            ctx.AddObject(currentTable.Name, insertEntity);
            ctx.SaveChangesWithRetries();


            // Attempt Insert Conflict Entity
            TableServiceContext ctx2 = tableClient.GetTableServiceContext();
            ComplexEntity conflictEntity = new ComplexEntity("insert test", "foo" + format.ToString());
            ctx2.AddObject(currentTable.Name, insertEntity);
            OperationContext opContext = new OperationContext();

            try
            {
                ctx2.SaveChangesWithRetries(SaveChangesOptions.None, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.Conflict, new string[] { "EntityAlreadyExists" }, "The specified entity already exists");
            }
        }

        #endregion

        #region Insert Or Merge

        [TestMethod]
        [Description("Single Entity Insert Or Merge")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertOrMerge()
        {
            DoSingleEntityInsertOrMerge(TablePayloadFormat.Json);
        }

        private void DoSingleEntityInsertOrMerge(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext queryContext = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);
            SetPayloadFormatOnDataServiceContext(queryContext, format, tableClient);

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo" + format.ToString());

            // Insert Or Merge with no pre-existing entity
            MergeEntity insertOrMergeEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            insertOrMergeEntity.Randomize();
            ctx.AttachTo(currentTable.Name, insertOrMergeEntity, null);
            ctx.UpdateObject(insertOrMergeEntity);
            ctx.SaveChangesWithRetries();
            ctx.Detach(insertOrMergeEntity);

            // Retrieve Entity & Verify Contents
            UnionEnitity retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                                            where ent.PartitionKey == baseEntity.PartitionKey
                                            && ent.RowKey == baseEntity.RowKey
                                            select ent).AsTableServiceQuery(ctx).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(insertOrMergeEntity.D, retrievedEntity.D);
            Assert.AreEqual(insertOrMergeEntity.E, retrievedEntity.E);
            Assert.AreEqual(insertOrMergeEntity.F, retrievedEntity.F);

            UnionEnitity mergedEntity = new UnionEnitity("insert test", "foo" + format.ToString());
            mergedEntity.Randomize();
            mergedEntity.D = insertOrMergeEntity.D;
            mergedEntity.E = insertOrMergeEntity.E;
            mergedEntity.F = insertOrMergeEntity.F;

            ctx.AttachTo(currentTable.Name, mergedEntity, null);
            ctx.UpdateObject(mergedEntity);
            ctx.SaveChangesWithRetries();
            ctx.Detach(mergedEntity);

            // Retrieve Entity & Verify
            retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                               where ent.PartitionKey == baseEntity.PartitionKey
                               && ent.RowKey == baseEntity.RowKey
                               select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(mergedEntity.A, retrievedEntity.A);
            Assert.AreEqual(mergedEntity.B, retrievedEntity.B);
            Assert.AreEqual(mergedEntity.C, retrievedEntity.C);
            Assert.AreEqual(mergedEntity.D, retrievedEntity.D);
            Assert.AreEqual(mergedEntity.E, retrievedEntity.E);
            Assert.AreEqual(mergedEntity.F, retrievedEntity.F);
        }
        #endregion

        #region Insert Or Replace

        [TestMethod]
        [Description("Single Entity Insert Or Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertOrReplace()
        {
            DoSingleEntityInsertOrReplace(TablePayloadFormat.Json);
        }

        private void DoSingleEntityInsertOrReplace(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext queryContext = tableClient.GetTableServiceContext();
            queryContext.MergeOption = MergeOption.NoTracking;
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);
            SetPayloadFormatOnDataServiceContext(queryContext, format, tableClient);

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo" + format.ToString());

            // Insert Or Merge with no pre-existing entity
            MergeEntity insertOrReplaceEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            insertOrReplaceEntity.Randomize();
            ctx.AttachTo(currentTable.Name, insertOrReplaceEntity, null);
            ctx.UpdateObject(insertOrReplaceEntity);
            ctx.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
            ctx.Detach(insertOrReplaceEntity);

            // Retrieve Entity & Verify Contents
            UnionEnitity retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                                            where ent.PartitionKey == baseEntity.PartitionKey
                                            && ent.RowKey == baseEntity.RowKey
                                            select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(null, retrievedEntity.A);
            Assert.AreEqual(null, retrievedEntity.B);
            Assert.AreEqual(null, retrievedEntity.C);
            Assert.AreEqual(insertOrReplaceEntity.D, retrievedEntity.D);
            Assert.AreEqual(insertOrReplaceEntity.E, retrievedEntity.E);
            Assert.AreEqual(insertOrReplaceEntity.F, retrievedEntity.F);

            BaseEntity replacedEntity = new BaseEntity("insert test", "foo" + format.ToString());
            replacedEntity.Randomize();

            ctx.AttachTo(currentTable.Name, replacedEntity, null);
            ctx.UpdateObject(replacedEntity);
            ctx.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);

            // Retrieve Entity & Verify
            retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                               where ent.PartitionKey == baseEntity.PartitionKey
                               && ent.RowKey == baseEntity.RowKey
                               select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replacedEntity.A, retrievedEntity.A);
            Assert.AreEqual(replacedEntity.B, retrievedEntity.B);
            Assert.AreEqual(replacedEntity.C, retrievedEntity.C);
            Assert.AreEqual(null, retrievedEntity.D);
            Assert.AreEqual(null, retrievedEntity.E);
            Assert.AreEqual(null, retrievedEntity.F);
        }

        [TestMethod]
        [Description("Single Entity Insert Or Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertOrReplaceAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext queryContext = tableClient.GetTableServiceContext();
            queryContext.MergeOption = MergeOption.NoTracking;

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo");

            // Insert Or Merge with no pre-existing entity
            MergeEntity insertOrReplaceEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            insertOrReplaceEntity.Randomize();
            ctx.AttachTo(currentTable.Name, insertOrReplaceEntity, null);
            ctx.UpdateObject(insertOrReplaceEntity);

            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                ctx.BeginSaveChangesWithRetries(
                    SaveChangesOptions.ReplaceOnUpdate,
                    (res) =>
                        {
                            asyncRes = res;
                            evt.Set();
                        },
                    null);
                evt.WaitOne();
                
                ctx.EndSaveChangesWithRetries(asyncRes);
            }

            ctx.Detach(insertOrReplaceEntity);

            // Retrieve Entity & Verify Contents
            UnionEnitity retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                                            where ent.PartitionKey == baseEntity.PartitionKey
                                            && ent.RowKey == baseEntity.RowKey
                                            select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(null, retrievedEntity.A);
            Assert.AreEqual(null, retrievedEntity.B);
            Assert.AreEqual(null, retrievedEntity.C);
            Assert.AreEqual(insertOrReplaceEntity.D, retrievedEntity.D);
            Assert.AreEqual(insertOrReplaceEntity.E, retrievedEntity.E);
            Assert.AreEqual(insertOrReplaceEntity.F, retrievedEntity.F);

            BaseEntity replacedEntity = new BaseEntity("insert test", "foo");
            replacedEntity.Randomize();

            ctx.AttachTo(currentTable.Name, replacedEntity, null);
            ctx.UpdateObject(replacedEntity);

            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                ctx.BeginSaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();
                
                ctx.EndSaveChangesWithRetries(asyncRes);
            }

            // Retrieve Entity & Verify
            retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                               where ent.PartitionKey == baseEntity.PartitionKey
                               && ent.RowKey == baseEntity.RowKey
                               select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replacedEntity.A, retrievedEntity.A);
            Assert.AreEqual(replacedEntity.B, retrievedEntity.B);
            Assert.AreEqual(replacedEntity.C, retrievedEntity.C);
            Assert.AreEqual(null, retrievedEntity.D);
            Assert.AreEqual(null, retrievedEntity.E);
            Assert.AreEqual(null, retrievedEntity.F);
        }

        [TestMethod]
        [Description("Test Table Save Changes Retry Basic APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableServiceContextSaveChangeWithRetriesAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // Get data context.
            TableServiceContext context = tableClient.GetTableServiceContext();

            // Create the new entity.
            BaseEntity entity = new BaseEntity("Hello", "world.");

            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                // Add the entity.
                context.AttachTo(this.currentTable.Name, entity);
                context.UpdateObject(entity);
                
                // Save changes to the service.
                IAsyncResult result = context.BeginSaveChangesWithRetries(ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                context.EndSaveChangesWithRetries(result);

                BaseEntity entity2 = new BaseEntity("Insert", "foo.");
                context.AttachTo(this.currentTable.Name, entity2);
                context.UpdateObject(entity2);

                // Test again with different parameters
                result = context.BeginSaveChangesWithRetries(SaveChangesOptions.None, ar => waitHandle.Set(), null);
                waitHandle.WaitOne();
                context.EndSaveChangesWithRetries(result);

                // Retrieve Entities & Verify
                BaseEntity retrievedEntity = (from ent in context.CreateQuery<BaseEntity>(currentTable.Name)
                                              where
                                                  ent.PartitionKey == entity2.PartitionKey
                                                  && ent.RowKey == entity2.RowKey
                                              select ent).AsTableServiceQuery(context).Execute().FirstOrDefault();

                Assert.IsNotNull(retrievedEntity);

                retrievedEntity = (from ent in context.CreateQuery<BaseEntity>(currentTable.Name)
                                              where
                                                  ent.PartitionKey == entity.PartitionKey
                                                  && ent.RowKey == entity.RowKey
                                              select ent).AsTableServiceQuery(context).Execute().FirstOrDefault();

                Assert.IsNotNull(retrievedEntity);
            }

            // Test Dispose
            context.Dispose();
        }

#if TASK
        [TestMethod]
        [Description("Single Entity Insert Or Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertOrReplaceTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext queryContext = tableClient.GetTableServiceContext();
            queryContext.MergeOption = MergeOption.NoTracking;

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo");

            // Insert Or Merge with no pre-existing entity
            MergeEntity insertOrReplaceEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            insertOrReplaceEntity.Randomize();
            ctx.AttachTo(currentTable.Name, insertOrReplaceEntity, null);
            ctx.UpdateObject(insertOrReplaceEntity);
            ctx.SaveChangesWithRetriesAsync(SaveChangesOptions.ReplaceOnUpdate).Wait();
            ctx.Detach(insertOrReplaceEntity);

            // Retrieve Entity & Verify Contents
            UnionEnitity retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                                            where ent.PartitionKey == baseEntity.PartitionKey
                                            && ent.RowKey == baseEntity.RowKey
                                            select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(null, retrievedEntity.A);
            Assert.AreEqual(null, retrievedEntity.B);
            Assert.AreEqual(null, retrievedEntity.C);
            Assert.AreEqual(insertOrReplaceEntity.D, retrievedEntity.D);
            Assert.AreEqual(insertOrReplaceEntity.E, retrievedEntity.E);
            Assert.AreEqual(insertOrReplaceEntity.F, retrievedEntity.F);

            BaseEntity replacedEntity = new BaseEntity("insert test", "foo");
            replacedEntity.Randomize();

            ctx.AttachTo(currentTable.Name, replacedEntity, null);
            ctx.UpdateObject(replacedEntity);
            ctx.SaveChangesWithRetriesAsync(SaveChangesOptions.ReplaceOnUpdate).Wait();

            // Retrieve Entity & Verify
            retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                               where ent.PartitionKey == baseEntity.PartitionKey
                               && ent.RowKey == baseEntity.RowKey
                               select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(replacedEntity.A, retrievedEntity.A);
            Assert.AreEqual(replacedEntity.B, retrievedEntity.B);
            Assert.AreEqual(replacedEntity.C, retrievedEntity.C);
            Assert.AreEqual(null, retrievedEntity.D);
            Assert.AreEqual(null, retrievedEntity.E);
            Assert.AreEqual(null, retrievedEntity.F);
        }
#endif

        #endregion

        #region Delete

        [TestMethod]
        [Description("Single Entity Delete")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityDelete()
        {
            DoSingleEntityDelete(TablePayloadFormat.Json);
        }

        private void DoSingleEntityDelete(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);

            // Insert Entity
            ComplexEntity insertEntity = new ComplexEntity("insert test", "foo");
            ctx.AddObject(currentTable.Name, insertEntity);
            ctx.SaveChangesWithRetries();

            // Retrieve Entity

            ComplexEntity retrievedEntity = (from ent in ctx.CreateQuery<ComplexEntity>(currentTable.Name)
                                             where ent.PartitionKey == insertEntity.PartitionKey
                                             && ent.RowKey == insertEntity.RowKey
                                             select ent).AsTableServiceQuery(ctx).Execute().FirstOrDefault();
            Assert.IsNotNull(retrievedEntity);
            ctx.DeleteObject(retrievedEntity);
            ctx.SaveChangesWithRetries();

            try
            {
                // Retrieve Entity
                retrievedEntity = (from ent in ctx.CreateQuery<ComplexEntity>(currentTable.Name)
                                   where ent.PartitionKey == insertEntity.PartitionKey
                                   && ent.RowKey == insertEntity.RowKey
                                   select ent).AsTableServiceQuery(ctx).Execute().FirstOrDefault();
                Assert.Fail();
            }
            catch (StorageException ex)
            {
                Assert.AreEqual(ex.RequestInformation.HttpStatusCode, (int)HttpStatusCode.NotFound);
            }
        }

        [TestMethod]
        [Description("Single Entity Delete Fail")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertDeleteFail()
        {
            DoSingleEntityInsertDeleteFail(TablePayloadFormat.Json);
        }

        private void DoSingleEntityInsertDeleteFail(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);

            // Delete Entity that does not exits
            ComplexEntity deleteEntity = new ComplexEntity("insert test", "foo" + format.ToString());
            ctx.AttachTo(currentTable.Name, deleteEntity, "*");
            ctx.DeleteObject(deleteEntity);
            OperationContext opContext = new OperationContext();

            try
            {
                ctx.SaveChangesWithRetries(SaveChangesOptions.None, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }


            ctx = tableClient.GetTableServiceContext();
            TableServiceContext ctx2 = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx2, format, tableClient);

            // Insert Entity
            ComplexEntity insertEntity = new ComplexEntity("insert test", "foo" + format.ToString());
            ctx.AddObject(currentTable.Name, insertEntity);
            ctx.SaveChangesWithRetries();

            // Update Entity
            ComplexEntity retrievedEntity = (from ent in ctx2.CreateQuery<ComplexEntity>(currentTable.Name)
                                             where ent.PartitionKey == insertEntity.PartitionKey
                                             && ent.RowKey == insertEntity.RowKey
                                             select ent).AsTableServiceQuery(ctx2).Execute().FirstOrDefault();

            retrievedEntity.String = "updated value";

            ctx2.UpdateObject(retrievedEntity);
            ctx2.SaveChangesWithRetries();

            // Now delete old reference with stale etag and validate exception
            ctx.DeleteObject(insertEntity);

            opContext = new OperationContext();
            try
            {
                ctx.SaveChangesWithRetries(SaveChangesOptions.None, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                    1,
                    (int)HttpStatusCode.PreconditionFailed,
                    new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                    new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }
        }
        #endregion

        #region Merge

        [TestMethod]
        [Description("Single Entity Merge")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityMerge()
        {
            DoSingleEntityMerge(TablePayloadFormat.Json);
        }

        private void DoSingleEntityMerge(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext queryContext = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);
            SetPayloadFormatOnDataServiceContext(queryContext, format, tableClient);

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo" + format.ToString());
            baseEntity.Randomize();
            ctx.AddObject(currentTable.Name, baseEntity);
            ctx.SaveChangesWithRetries();
            string etag = ctx.Entities.First().ETag;
            ctx.Detach(baseEntity);

            MergeEntity mergeEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            mergeEntity.Randomize();
            ctx.AttachTo(currentTable.Name, mergeEntity, etag);
            ctx.UpdateObject(mergeEntity);
            ctx.SaveChangesWithRetries();

            // Retrieve Entity
            UnionEnitity retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                                            where ent.PartitionKey == baseEntity.PartitionKey
                                            && ent.RowKey == baseEntity.RowKey
                                            select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(baseEntity.A, retrievedEntity.A);
            Assert.AreEqual(baseEntity.B, retrievedEntity.B);
            Assert.AreEqual(baseEntity.C, retrievedEntity.C);
            Assert.AreEqual(mergeEntity.D, retrievedEntity.D);
            Assert.AreEqual(mergeEntity.E, retrievedEntity.E);
            Assert.AreEqual(mergeEntity.F, retrievedEntity.F);
        }

        [TestMethod]
        [Description("Single Entity Merge Fail")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityMergeFail()
        {
            DoSingleEntityMergeFail(TablePayloadFormat.Json);
        }

        private void DoSingleEntityMergeFail(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext secondContext = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);
            SetPayloadFormatOnDataServiceContext(secondContext, format, tableClient);

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo" + format.ToString());
            baseEntity.Randomize();
            ctx.AddObject(currentTable.Name, baseEntity);
            ctx.SaveChangesWithRetries();
            string etag = ctx.Entities.First().ETag;
            baseEntity.A = "updated";
            ctx.UpdateObject(baseEntity);
            ctx.SaveChangesWithRetries();

            MergeEntity mergeEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            mergeEntity.Randomize();
            secondContext.AttachTo(currentTable.Name, mergeEntity, etag);
            secondContext.UpdateObject(mergeEntity);
            OperationContext opContext = new OperationContext();

            try
            {
                secondContext.SaveChangesWithRetries(SaveChangesOptions.None, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                      1,
                      (int)HttpStatusCode.PreconditionFailed,
                      new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                      new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            ctx.DeleteObject(baseEntity);
            ctx.SaveChangesWithRetries();

            opContext = new OperationContext();

            // try merging with deleted entity
            try
            {
                secondContext.SaveChangesWithRetries(SaveChangesOptions.None, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }
        #endregion

        #region Replace

        [TestMethod]
        [Description("Single Entity Replace")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityReplace()
        {
            DoSingleEntityReplace(TablePayloadFormat.Json);
        }

        private void DoSingleEntityReplace(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext queryContext = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);
            SetPayloadFormatOnDataServiceContext(queryContext, format, tableClient);

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo" + format.ToString());
            baseEntity.Randomize();
            ctx.AddObject(currentTable.Name, baseEntity);
            ctx.SaveChangesWithRetries();
            string etag = ctx.Entities.First().ETag;
            ctx.Detach(baseEntity);

            MergeEntity replaceEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            replaceEntity.Randomize();
            ctx.AttachTo(currentTable.Name, replaceEntity, etag);
            ctx.UpdateObject(replaceEntity);
            ctx.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);

            // Retrieve Entity
            UnionEnitity retrievedEntity = (from ent in queryContext.CreateQuery<UnionEnitity>(currentTable.Name)
                                            where ent.PartitionKey == baseEntity.PartitionKey
                                            && ent.RowKey == baseEntity.RowKey
                                            select ent).AsTableServiceQuery(queryContext).Execute().FirstOrDefault();

            Assert.IsNotNull(retrievedEntity);
            Assert.AreEqual(null, retrievedEntity.A);
            Assert.AreEqual(null, retrievedEntity.B);
            Assert.AreEqual(null, retrievedEntity.C);
            Assert.AreEqual(replaceEntity.D, retrievedEntity.D);
            Assert.AreEqual(replaceEntity.E, retrievedEntity.E);
            Assert.AreEqual(replaceEntity.F, retrievedEntity.F);
        }

        [TestMethod]
        [Description("Single Entity Replace Fail")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityReplaceFail()
        {
            DoSingleEntityReplaceFail(TablePayloadFormat.Json);
        }

        private void DoSingleEntityReplaceFail(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            TableServiceContext secondContext = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);
            SetPayloadFormatOnDataServiceContext(secondContext, format, tableClient);

            // Insert Entity
            BaseEntity baseEntity = new BaseEntity("insert test", "foo" + format.ToString());
            baseEntity.Randomize();
            ctx.AddObject(currentTable.Name, baseEntity);
            ctx.SaveChangesWithRetries();
            string etag = ctx.Entities.First().ETag;
            baseEntity.A = "updated";
            ctx.UpdateObject(baseEntity);
            ctx.SaveChangesWithRetries();

            MergeEntity replaceEntity = new MergeEntity(baseEntity.PartitionKey, baseEntity.RowKey);
            replaceEntity.Randomize();
            secondContext.AttachTo(currentTable.Name, replaceEntity, etag);
            secondContext.UpdateObject(replaceEntity);
            OperationContext opContext = new OperationContext();

            try
            {
                secondContext.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext,
                       1,
                       (int)HttpStatusCode.PreconditionFailed,
                       new string[] { "UpdateConditionNotSatisfied", "ConditionNotMet" },
                       new string[] { "The update condition specified in the request was not satisfied.", "The condition specified using HTTP conditional header(s) is not met." });
            }

            ctx.DeleteObject(baseEntity);
            ctx.SaveChangesWithRetries();

            opContext = new OperationContext();

            // try merging with deleted entity
            try
            {
                secondContext.SaveChangesWithRetries(SaveChangesOptions.None, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.NotFound, new string[] { "ResourceNotFound" }, "The specified resource does not exist.");
            }
        }

        #endregion

        #region Batch

        [TestMethod]
        [Description("Batch Insert")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BatchInsert()
        {
            DoBatchInsert(TablePayloadFormat.Json);
        }

        private void DoBatchInsert(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);

            // Insert Entities
            SortedDictionary<string, ComplexEntity> entities = new SortedDictionary<string, ComplexEntity>();
            for (int i = 0; i < 100; i++)
            {
                ComplexEntity insertEntity = new ComplexEntity("insert test" + format.ToString(), "foo" + format.ToString() + i);
                entities.Add(insertEntity.RowKey, insertEntity);
                ctx.AddObject(currentTable.Name, insertEntity);
            }

            DataServiceResponse response = ctx.SaveChangesWithRetries(SaveChangesOptions.Batch);
            Assert.AreEqual((int)HttpStatusCode.Accepted, response.BatchStatusCode);

            // Retrieve Entities
            List<ComplexEntity> retrievedEntities = (from ent in ctx.CreateQuery<ComplexEntity>(currentTable.Name)
                                                     where ent.PartitionKey == entities.First().Value.PartitionKey
                                                     select ent).AsTableServiceQuery(ctx).Execute().ToList();

            Assert.AreEqual(entities.Count, retrievedEntities.Count);

            foreach (ComplexEntity retrievedEntity in retrievedEntities)
            {
                ComplexEntity.AssertEquality(entities[retrievedEntity.RowKey], retrievedEntity);
                entities.Remove(retrievedEntity.RowKey);
            }

            Assert.AreEqual(0, entities.Count);
        }

        [TestMethod]
        [Description("Batch Insert")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BatchInsertAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();

            // Insert Entities
            SortedDictionary<string, ComplexEntity> entities = new SortedDictionary<string, ComplexEntity>();
            for (int i = 0; i < 100; i++)
            {
                ComplexEntity insertEntity = new ComplexEntity("insert test", "foo" + i);
                entities.Add(insertEntity.RowKey, insertEntity);
                ctx.AddObject(currentTable.Name, insertEntity);
            }

            DataServiceResponse response;
            using (ManualResetEvent evt = new ManualResetEvent(false))
            {
                IAsyncResult asyncRes = null;
                ctx.BeginSaveChangesWithRetries(SaveChangesOptions.Batch, (res) =>
                {
                    asyncRes = res;
                    evt.Set();
                }, null);
                evt.WaitOne();

                response = ctx.EndSaveChangesWithRetries(asyncRes);
            }

            Assert.AreEqual((int)HttpStatusCode.Accepted, response.BatchStatusCode);

            // Retrieve Entities
            List<ComplexEntity> retrievedEntities = (from ent in ctx.CreateQuery<ComplexEntity>(currentTable.Name)
                                                     where ent.PartitionKey == entities.First().Value.PartitionKey
                                                     select ent).AsTableServiceQuery(ctx).Execute().ToList();

            Assert.AreEqual(entities.Count, retrievedEntities.Count);

            foreach (ComplexEntity retrievedEntity in retrievedEntities)
            {
                ComplexEntity.AssertEquality(entities[retrievedEntity.RowKey], retrievedEntity);
                entities.Remove(retrievedEntity.RowKey);
            }

            Assert.AreEqual(0, entities.Count);
        }

#if TASK
        [TestMethod]
        [Description("Batch Insert")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BatchInsertTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();

            // Insert Entities
            SortedDictionary<string, ComplexEntity> entities = new SortedDictionary<string, ComplexEntity>();
            for (int i = 0; i < 100; i++)
            {
                ComplexEntity insertEntity = new ComplexEntity("insert test", "foo" + i);
                entities.Add(insertEntity.RowKey, insertEntity);
                ctx.AddObject(currentTable.Name, insertEntity);
            }

            DataServiceResponse response = ctx.SaveChangesWithRetriesAsync(SaveChangesOptions.Batch).Result;
            Assert.AreEqual((int)HttpStatusCode.Accepted, response.BatchStatusCode);

            // Retrieve Entities
            List<ComplexEntity> retrievedEntities = (from ent in ctx.CreateQuery<ComplexEntity>(currentTable.Name)
                                                     where ent.PartitionKey == entities.First().Value.PartitionKey
                                                     select ent).AsTableServiceQuery(ctx).Execute().ToList();

            Assert.AreEqual(entities.Count, retrievedEntities.Count);

            foreach (ComplexEntity retrievedEntity in retrievedEntities)
            {
                ComplexEntity.AssertEquality(entities[retrievedEntity.RowKey], retrievedEntity);
                entities.Remove(retrievedEntity.RowKey);
            }

            Assert.AreEqual(0, entities.Count);
        }
#endif

        #endregion

        #region Insert Negative Tests

        [TestMethod]
        [Description("Single Entity Insert Entity over 1 MB")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void SingleEntityInsertEntityOver1MB()
        {
            DoSingleEntityInsertEntityOver1MB(TablePayloadFormat.Json);
        }

        private void DoSingleEntityInsertEntityOver1MB(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableServiceContext ctx = tableClient.GetTableServiceContext();
            SetPayloadFormatOnDataServiceContext(ctx, format, tableClient);

            // Insert Entity
            ComplexEntity insertEntity = new ComplexEntity("insert test", "foo");
            insertEntity.Binary = new byte[1024 * 1024];
            ctx.AddObject(currentTable.Name, insertEntity);
            OperationContext opContext = new OperationContext();

            try
            {
                ctx.SaveChangesWithRetries(SaveChangesOptions.None, null, opContext);
                Assert.Fail();
            }
            catch (StorageException)
            {
                TestHelper.ValidateResponse(opContext, 1, (int)HttpStatusCode.BadRequest, new string[] { "EntityTooLarge" }, "The entity is larger than the maximum allowed size (1MB).");
            }
        }
        #endregion
    }
}
