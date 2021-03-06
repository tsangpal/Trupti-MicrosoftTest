// -----------------------------------------------------------------------------------------
// <copyright file="BlobWriteStreamTest.cs" company="Microsoft">
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class BlobWriteStreamTest : BlobTestBase
    {
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Create blobs using blob stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlobWriteStreamOpenAndCloseAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                // Block blob tests
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("blob1");
                using (Stream writeStream = await blockBlob.OpenWriteAsync())
                {
                }

                CloudBlockBlob blockBlob2 = container.GetBlockBlobReference(blockBlob.Name);
                await blockBlob2.FetchAttributesAsync();
                Assert.AreEqual(0, blockBlob2.Properties.Length);
                Assert.AreEqual(BlobType.BlockBlob, blockBlob2.Properties.BlobType);

                // Page blob tests
                CloudPageBlob pageBlob = container.GetPageBlobReference("blob2");
                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await pageBlob.OpenWriteAsync(null, null, null, opContext),
                    opContext,
                    "Opening a page blob stream with no size should fail on a blob that does not exist",
                    HttpStatusCode.NotFound);
                using (Stream writeStream = await pageBlob.OpenWriteAsync(1024))
                {
                }
                using (Stream writeStream = await pageBlob.OpenWriteAsync(null))
                {
                }

                CloudPageBlob pageBlob2 = container.GetPageBlobReference(pageBlob.Name);
                await pageBlob2.FetchAttributesAsync();
                Assert.AreEqual(1024, pageBlob2.Properties.Length);
                Assert.AreEqual(BlobType.PageBlob, pageBlob2.Properties.BlobType);

                // Append blob test
                CloudAppendBlob appendBlob = container.GetAppendBlobReference("blob3");
                using (Stream writeStream = await appendBlob.OpenWriteAsync(true))
                {
                }

                CloudAppendBlob appendBlob2 = container.GetAppendBlobReference(appendBlob.Name);
                await appendBlob2.FetchAttributesAsync();
                Assert.AreEqual(0, appendBlob2.Properties.Length);
                Assert.AreEqual(BlobType.AppendBlob, appendBlob2.Properties.BlobType);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamOpenWithAccessConditionAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudBlockBlob existingBlob = container.GetBlockBlobReference("blob");
                await existingBlob.PutBlockListAsync(new List<string>());

                CloudBlockBlob blob = container.GetBlockBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.NotFound);

                blob = container.GetBlockBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetBlockBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.NotModified);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                await TestHelper.ExpectedExceptionAsync(
                    () =>
                    {
                        blobStream.Dispose();
                        return Task.FromResult(true);
                    },
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.NotModified);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                await existingBlob.SetPropertiesAsync();
                await TestHelper.ExpectedExceptionAsync(
                    () =>
                    {
                        blobStream.Dispose();
                        return Task.FromResult(true);
                    },
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetBlockBlobReference("blob7");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(accessCondition, null, context);
                await blob.PutBlockListAsync(new List<string>());
                await TestHelper.ExpectedExceptionAsync(
                    () =>
                    {
                        blobStream.Dispose();
                        return Task.FromResult(true);
                    },
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                blob = container.GetBlockBlobReference("blob8");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value);
                blobStream = await existingBlob.OpenWriteAsync(accessCondition, null, context);
                await existingBlob.SetPropertiesAsync();
                await TestHelper.ExpectedExceptionAsync(
                    () =>
                    {
                        blobStream.Dispose();
                        return Task.FromResult(true);
                    },
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamOpenWithAccessConditionAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudPageBlob existingBlob = container.GetPageBlobReference("blob");
                await existingBlob.CreateAsync(1024);

                CloudPageBlob blob = container.GetPageBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetPageBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                Stream blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetPageBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await blob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await existingBlob.OpenWriteAsync(1024, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a blob using blob stream by specifying an access condition")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamOpenWithAccessConditionAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudAppendBlob existingBlob = container.GetAppendBlobReference("blob");
                await existingBlob.CreateOrReplaceAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                blob = container.GetAppendBlobReference("blob3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                var blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                blob = container.GetAppendBlobReference("blob6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await blob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingBlob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingBlob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "BlobWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(1));
                blobStream = await existingBlob.OpenWriteAsync(true, accessCondition, null, context);
                blobStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingBlob.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingBlob.OpenWriteAsync(true, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudBlobClient blobClient = GenerateCloudBlobClient();
            blobClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            string name = GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(name);
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (Stream writeStream = await blob.OpenWriteAsync())
                    {
                        Stream blobStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }

                        await blobStream.FlushAsync();
                    }

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload a block blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamSeekTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (Stream writeStream = await blob.OpenWriteAsync())
                {
                    Stream blobStream = writeStream;
                    TestHelper.ExpectedException<NotSupportedException>(
                        () => blobStream.Seek(1, SeekOrigin.Begin),
                        "Block blob write stream should not be seekable");
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task BlockBlobWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = await blob.OpenWriteAsync(null, null, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(1, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await Task.Factory.FromAsync(blobStream.BeginCommit, blobStream.EndCommit, null);

                        Assert.AreEqual(4, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload a page blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(6 * 512);

            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 8 * 512;

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (Stream writeStream = await blob.OpenWriteAsync(buffer.Length * 3))
                    {
                        Stream blobStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }

                        await blobStream.FlushAsync();
                    }

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }

                    using (Stream writeStream = await blob.OpenWriteAsync(null))
                    {
                        Stream blobStream = writeStream;
                        blobStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                        wholeBlob.Seek(buffer.Length / 2, SeekOrigin.Begin);

                        for (int i = 0; i < 2; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }

                        await blobStream.FlushAsync();
                    }

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload a page blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamRandomSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            container.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (Stream writeStream = await blob.OpenWriteAsync(buffer.Length))
                    {
                        Stream blobStream = writeStream;
                        TestHelper.ExpectedException<ArgumentOutOfRangeException>(
                            () => blobStream.Seek(1, SeekOrigin.Begin),
                            "Page blob stream should not allow unaligned seeks");

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        Random random = new Random();
                        for (int i = 0; i < 10; i++)
                        {
                            int offset = random.Next(buffer.Length / 512) * 512;
                            TestHelper.SeekRandomly(blobStream, offset);
                            await blobStream.WriteAsync(buffer, 0, buffer.Length - offset);
                            wholeBlob.Seek(offset, SeekOrigin.Begin);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length - offset);
                        }
                    }

                    await blob.FetchAttributesAsync();
                    Assert.IsNull(blob.Properties.ContentMD5);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task PageBlobWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = await blob.OpenWriteAsync(4 * 512, null, null, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await Task.Factory.FromAsync(blobStream.BeginCommit, blobStream.EndCommit, null);

                        Assert.AreEqual(4, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload an append blob using blob stream and verify contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudBlobClient blobClient = GenerateCloudBlobClient();
            string name = GetRandomContainerName();
            CloudBlobContainer container = blobClient.GetContainerReference(name);
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (Stream writeStream = await blob.OpenWriteAsync(true))
                    {
                        Stream blobStream = writeStream;

                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeBlob.Position, blobStream.Position);
                        }

                        await blobStream.FlushAsync();
                    }

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Check seek functionality.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamSeekTestAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                using (Stream writeStream = await blob.OpenWriteAsync(true))
                {
                    Stream blobStream = writeStream;
                    TestHelper.ExpectedException<NotSupportedException>(
                        () => blobStream.Seek(1, SeekOrigin.Begin),
                        "Append blob write stream should not be seekable");
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's flush functionality")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task AppendBlobWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudAppendBlob blob = container.GetAppendBlobReference("blob1");
                blob.StreamWriteSizeInBytes = 1 * 1024 * 1024;
                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    OperationContext opContext = new OperationContext();
                    using (CloudBlobStream blobStream = await blob.OpenWriteAsync(true, null, null, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await blobStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeBlob.WriteAsync(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await blobStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeBlob.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await Task.Factory.FromAsync(blobStream.BeginCommit, blobStream.EndCommit, null);

                        Assert.AreEqual(4, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(downloadedBlob);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
