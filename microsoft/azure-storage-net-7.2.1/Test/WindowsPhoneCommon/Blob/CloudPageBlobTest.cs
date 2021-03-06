// -----------------------------------------------------------------------------------------
// <copyright file="CloudPageBlobTest.cs" company="Microsoft">
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
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class CloudPageBlobTest : BlobTestBase
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
        [Description("Create a zero-length page blob and then delete it")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCreateAndDeleteAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(0);
                Assert.IsTrue(await blob.ExistsAsync());
                await blob.DeleteAsync();
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Resize a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobResizeAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                await blob.CreateAsync(1024);
                Assert.AreEqual(1024, blob.Properties.Length);
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1024, blob2.Properties.Length);
                blob2.Properties.ContentType = "text/plain";
                await blob2.SetPropertiesAsync();
                await blob.ResizeAsync(2048);
                Assert.AreEqual(2048, blob.Properties.Length);
                await blob.FetchAttributesAsync();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(2048, blob2.Properties.Length);

                // Resize to 0 length
                await blob.ResizeAsync(0);
                Assert.AreEqual(0, blob.Properties.Length);
                await blob.FetchAttributesAsync();
                Assert.AreEqual("text/plain", blob.Properties.ContentType);
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(0, blob2.Properties.Length);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Use sequence number conditions on a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSequenceNumberAsync()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");

                await blob.CreateAsync(buffer.Length);
                Assert.IsNull(blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Update, 5);
                Assert.AreEqual(5, blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Max, 7);
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Max, 3);
                Assert.AreEqual(7, blob.Properties.PageBlobSequenceNumber);

                await blob.SetSequenceNumberAsync(SequenceNumberAction.Increment, null);
                Assert.AreEqual(8, blob.Properties.PageBlobSequenceNumber);

                StorageException e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Update, null),
                    "SetSequenceNumber with Update should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Update, -1),
                    "Negative sequence numbers are not supported");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentOutOfRangeException));

                e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Max, null),
                    "SetSequenceNumber with Max should require a value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));

                e = await TestHelper.ExpectedExceptionAsync<StorageException>(
                    async () => await blob.SetSequenceNumberAsync(SequenceNumberAction.Increment, 1),
                    "SetSequenceNumber with Increment should require null value");
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentException));

                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(8), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(8), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null);
                    await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    OperationContext context = new OperationContext();
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.ClearPagesAsync(0, stream.Length, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, context),
                        context,
                        "Sequence number condition should cause Put Page to fail",
                        HttpStatusCode.PreconditionFailed,
                        "SequenceNumberConditionNotMet");

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberEqualCondition(9), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberLessThanOrEqualCondition(7), null, null);

                    stream.Seek(0, SeekOrigin.Begin);
                    await blob.UploadFromStreamAsync(stream, AccessCondition.GenerateIfSequenceNumberLessThanCondition(8), null, null);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try to delete a non-existing page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobDeleteIfExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                Assert.IsFalse(await blob.DeleteIfExistsAsync());
                await blob.CreateAsync(0);
                Assert.IsTrue(await blob.DeleteIfExistsAsync());
                Assert.IsFalse(await blob.DeleteIfExistsAsync());
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Check a blob's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();

            try
            {
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");

                Assert.IsFalse(await blob2.ExistsAsync());

                await blob.CreateAsync(2048);

                Assert.IsTrue(await blob2.ExistsAsync());
                Assert.AreEqual(2048, blob2.Properties.Length);

                await blob.DeleteAsync();

                Assert.IsFalse(await blob2.ExistsAsync());
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify the attributes of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobFetchAttributesAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                Assert.AreEqual(1024, blob.Properties.Length);
                Assert.IsNotNull(blob.Properties.ETag);
                Assert.IsTrue(blob.Properties.LastModified > DateTimeOffset.UtcNow.AddMinutes(-5));
                Assert.IsNull(blob.Properties.CacheControl);
                Assert.IsNull(blob.Properties.ContentEncoding);
                Assert.IsNull(blob.Properties.ContentLanguage);
                Assert.IsNull(blob.Properties.ContentType);
                Assert.IsNull(blob.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unspecified, blob.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob.Properties.BlobType);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1024, blob2.Properties.Length);
                Assert.AreEqual(blob.Properties.ETag, blob2.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, blob2.Properties.LastModified);
                Assert.IsNull(blob2.Properties.ContentEncoding);
                Assert.IsNull(blob2.Properties.ContentLanguage);
                Assert.AreEqual("application/octet-stream", blob2.Properties.ContentType);
                Assert.IsNull(blob2.Properties.ContentMD5);
                Assert.AreEqual(LeaseStatus.Unlocked, blob2.Properties.LeaseStatus);
                Assert.AreEqual(BlobType.PageBlob, blob2.Properties.BlobType);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify setting the properties of a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSetPropertiesAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                string eTag = blob.Properties.ETag;
                DateTimeOffset lastModified = blob.Properties.LastModified.Value;

                await Task.Delay(1000);

                blob.Properties.CacheControl = "no-transform";
                blob.Properties.ContentEncoding = "gzip";
                blob.Properties.ContentLanguage = "tr,en";
                blob.Properties.ContentMD5 = "MDAwMDAwMDA=";
                blob.Properties.ContentType = "text/html";
                await blob.SetPropertiesAsync();
                Assert.IsTrue(blob.Properties.LastModified > lastModified);
                Assert.AreNotEqual(eTag, blob.Properties.ETag);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual("no-transform", blob2.Properties.CacheControl);
                Assert.AreEqual("gzip", blob2.Properties.ContentEncoding);
                Assert.AreEqual("tr,en", blob2.Properties.ContentLanguage);
                Assert.AreEqual("MDAwMDAwMDA=", blob2.Properties.ContentMD5);
                Assert.AreEqual("text/html", blob2.Properties.ContentType);

                CloudPageBlob blob3 = container.GetPageBlobReference("blob1");
                using (MemoryStream stream = new MemoryStream())
                {
                    BlobRequestOptions options = new BlobRequestOptions()
                    {
                        DisableContentMD5Validation = true,
                    };
                    await blob3.DownloadToStreamAsync(stream, null, options, null);
                }
                AssertAreEqual(blob2.Properties, blob3.Properties);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null);
                CloudPageBlob blob4 = (CloudPageBlob)results.Results.First();
                AssertAreEqual(blob2.Properties, blob4.Properties);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Try retrieving properties of a block blob using a page blob reference")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobFetchAttributesInvalidTypeAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);

                CloudBlockBlob blob2 = container.GetBlockBlobReference("blob1");
                OperationContext operationContext = new OperationContext();

                Assert.ThrowsException<AggregateException>(
                    () => blob2.FetchAttributesAsync(null, null, operationContext).Wait(),
                    "Fetching attributes of a page blob using a block blob reference should fail");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(InvalidOperationException));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that creating a page blob can also set its metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobCreateWithMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                blob.Metadata["key1"] = "value1";
                await blob.CreateAsync(1024);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Verify that a page blob's metadata can be updated")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSetMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);

                CloudPageBlob blob2 = container.GetPageBlobReference("blob1");
                await blob2.FetchAttributesAsync();
                Assert.AreEqual(0, blob2.Metadata.Count);

                OperationContext operationContext = new OperationContext();
                blob.Metadata["key1"] = null;

                Assert.ThrowsException<AggregateException>(
                    () => blob.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-null value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "";
                Assert.ThrowsException<AggregateException>(
                    () => blob.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-empty value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = " ";
                Assert.ThrowsException<AggregateException>(
                    () => blob.SetMetadataAsync(null, null, operationContext).Wait(),
                    "Metadata keys should have a non-whitespace only value");
                Assert.IsInstanceOfType(operationContext.LastResult.Exception.InnerException, typeof(ArgumentException));

                blob.Metadata["key1"] = "value1";
                await blob.SetMetadataAsync();

                await blob2.FetchAttributesAsync();
                Assert.AreEqual(1, blob2.Metadata.Count);
                Assert.AreEqual("value1", blob2.Metadata["key1"]);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.Metadata, null, null, null, null);
                CloudPageBlob blob3 = (CloudPageBlob)results.Results.First();
                Assert.AreEqual(1, blob3.Metadata.Count);
                Assert.AreEqual("value1", blob3.Metadata["key1"]);

                blob.Metadata.Clear();
                await blob.SetMetadataAsync();

                await blob2.FetchAttributesAsync();
                Assert.AreEqual(0, blob2.Metadata.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload/clear pages in a page blob and then verify page ranges")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobGetPageRangesAsync()
        {
            byte[] buffer = GetRandomBuffer(1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(4 * 1024);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await blob.WritePagesAsync(memoryStream, 512, null);
                }

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    await blob.WritePagesAsync(memoryStream, 3 * 1024, null);
                }

                await blob.ClearPagesAsync(1024, 1024);
                await blob.ClearPagesAsync(0, 512);

                IEnumerable<PageRange> pageRanges = await blob.GetPageRangesAsync();
                List<string> expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 4 * 1024 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                pageRanges = await blob.GetPageRangesAsync(1024, 1024, null, null, null);
                Assert.AreEqual(0, pageRanges.Count());

                pageRanges = await blob.GetPageRangesAsync(512, 3 * 1024, null, null, null);
                expectedPageRanges = new List<string>()
                {
                    new PageRange(512, 1023).ToString(),
                    new PageRange(3 * 1024, 7 * 512 - 1).ToString(),
                };
                foreach (PageRange pageRange in pageRanges)
                {
                    Assert.IsTrue(expectedPageRanges.Remove(pageRange.ToString()));
                }
                Assert.AreEqual(0, expectedPageRanges.Count);

                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.GetPageRangesAsync(1024, null, null, null, opContext),
                    opContext,
                    "Get Page Ranges with an offset but no count should fail",
                    HttpStatusCode.Unused);
                Assert.IsInstanceOfType(opContext.LastResult.Exception.InnerException, typeof(ArgumentNullException));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload pages to a page blob and then verify the contents")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobWritePagesAsync()
        {
            byte[] buffer = GetRandomBuffer(4 * 1024 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(4 * 1024 * 1024);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(memoryStream, 0, null),
                        "Zero-length WritePages should fail");

                    memoryStream.SetLength(4 * 1024 * 1024 + 1);
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(memoryStream, 0, null),
                        ">4MB WritePages should fail");
                }

                using (MemoryStream resultingData = new MemoryStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        OperationContext opContext = new OperationContext();
                        await TestHelper.ExpectedExceptionAsync(
                            async () => await blob.WritePagesAsync(memoryStream, 512, null, null, null, opContext),
                            opContext,
                            "Writing out-of-range pages should fail",
                            HttpStatusCode.RequestedRangeNotSatisfiable,
                            "InvalidPageRange");

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await blob.WritePagesAsync(memoryStream, 0, null);
                        resultingData.Write(buffer, 0, buffer.Length);

                        int offset = buffer.Length - 1024;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await blob.WritePagesAsync(memoryStream, 0, null);
                        resultingData.Seek(0, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);

                        offset = buffer.Length - 2048;
                        memoryStream.Seek(offset, SeekOrigin.Begin);
                        await blob.WritePagesAsync(memoryStream, 1024, null);
                        resultingData.Seek(1024, SeekOrigin.Begin);
                        resultingData.Write(buffer, offset, buffer.Length - offset);
                    }

                    using (MemoryStream blobData = new MemoryStream())
                    {
                        await blob.DownloadToStreamAsync(blobData);
                        Assert.AreEqual(resultingData.Length, blobData.Length);

                        Assert.IsTrue(blobData.ToArray().SequenceEqual(resultingData.ToArray()));
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamWithAccessConditionAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                AccessCondition accessCondition = AccessCondition.GenerateIfNoneMatchCondition("\"*\"");
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, null, accessCondition, operationContext, 0);

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, null, accessCondition, operationContext, 0),
                    operationContext,
                    "Uploading a blob on top of an existing blob should fail if the ETag matches",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, null, accessCondition, operationContext, 0);

                blob = container.GetPageBlobReference("blob3");
                await blob.CreateAsync(1024);
                accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, null, accessCondition, operationContext, 0),
                    operationContext,
                    "Uploading a blob on top of an non-existing blob should fail when the ETag doesn't match",
                    HttpStatusCode.PreconditionFailed);
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(blob.Properties.ETag);
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, null, accessCondition, operationContext, 0);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, null, null, null, 0);
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, null, null, null, 1024);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamLengthAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                // Upload half
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, 3 * 512, null, null, 0);
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, 3 * 512, null, null, 1024);

                // Upload full stream
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, 6 * 512, null, null, 0);
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, 4 * 512, null, null, 1024);

                // Exclude last page
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, 5 * 512, null, null, 0);
                await this.CloudPageBlobUploadFromStreamAsync(container, 6 * 512, 3 * 512, null, null, 1024);
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Single put blob and get blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadFromStreamLengthInvalidAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            try
            {
                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudPageBlobUploadFromStreamAsync(container, 3 * 512, 4 * 512, null, null, 0),
                        "The given stream does not contain the requested number of bytes from its given position.");

                await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await this.CloudPageBlobUploadFromStreamAsync(container, 3 * 512, 2 * 512, null, null, 1024),
                        "The given stream does not contain the requested number of bytes from its given position.");
            }
            finally
            {
                container.DeleteAsync().Wait();
            }
        }

        private async Task CloudPageBlobUploadFromStreamAsync(CloudBlobContainer container, int size, long? copyLength, AccessCondition accessCondition, OperationContext operationContext, int startOffset)
        {
            byte[] buffer = GetRandomBuffer(size);

            CloudPageBlob blob = container.GetPageBlobReference("blob1");
            blob.StreamWriteSizeInBytes = 512;

            using (MemoryStream originalBlobStream = new MemoryStream())
            {
                originalBlobStream.Write(buffer, startOffset, buffer.Length - startOffset);

                using (MemoryStream sourceStream = new MemoryStream(buffer))
                {
                    sourceStream.Seek(startOffset, SeekOrigin.Begin);
                    if (copyLength.HasValue)
                    {
                        await blob.UploadFromStreamAsync(sourceStream, copyLength.Value, accessCondition, null, operationContext); 
                    }
                    else
                    {
                        await blob.UploadFromStreamAsync(sourceStream, accessCondition, null, operationContext); 
                    }
                }

                using (MemoryStream downloadedBlobStream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(downloadedBlobStream);
                    Assert.AreEqual(copyLength ?? originalBlobStream.Length, downloadedBlobStream.Length);
                    TestHelper.AssertStreamsAreEqualAtIndex(
                        originalBlobStream,
                        downloadedBlobStream,
                        0,
                        0,
                        copyLength.HasValue ? (int)copyLength : (int)originalBlobStream.Length);
                }
            }
        }

        [TestMethod]
        [Description("Create snapshots of a page blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSnapshotAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                MemoryStream originalData = new MemoryStream(GetRandomBuffer(1024));
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.UploadFromStreamAsync(originalData);
                Assert.IsFalse(blob.IsSnapshot);
                Assert.IsNull(blob.SnapshotTime, "Root blob has SnapshotTime set");
                Assert.IsFalse(blob.SnapshotQualifiedUri.Query.Contains("snapshot"));
                Assert.AreEqual(blob.Uri, blob.SnapshotQualifiedUri);

                CloudPageBlob snapshot1 = await blob.CreateSnapshotAsync();
                Assert.AreEqual(blob.Properties.ETag, snapshot1.Properties.ETag);
                Assert.AreEqual(blob.Properties.LastModified, snapshot1.Properties.LastModified);
                Assert.IsTrue(snapshot1.IsSnapshot);
                Assert.IsNotNull(snapshot1.SnapshotTime, "Snapshot does not have SnapshotTime set");
                Assert.AreEqual(blob.Uri, snapshot1.Uri);
                Assert.AreNotEqual(blob.SnapshotQualifiedUri, snapshot1.SnapshotQualifiedUri);
                Assert.AreNotEqual(snapshot1.Uri, snapshot1.SnapshotQualifiedUri);
                Assert.IsTrue(snapshot1.SnapshotQualifiedUri.Query.Contains("snapshot"));

                CloudPageBlob snapshot2 = await blob.CreateSnapshotAsync();
                Assert.IsTrue(snapshot2.SnapshotTime.Value > snapshot1.SnapshotTime.Value);

                await snapshot1.FetchAttributesAsync();
                await snapshot2.FetchAttributesAsync();
                await blob.FetchAttributesAsync();
                AssertAreEqual(snapshot1.Properties, blob.Properties);

                CloudPageBlob snapshot1Clone = new CloudPageBlob(new Uri(blob.Uri + "?snapshot=" + snapshot1.SnapshotTime.Value.ToString("O")), blob.ServiceClient.Credentials);
                Assert.IsNotNull(snapshot1Clone.SnapshotTime, "Snapshot clone does not have SnapshotTime set");
                Assert.AreEqual(snapshot1.SnapshotTime.Value, snapshot1Clone.SnapshotTime.Value);
                await snapshot1Clone.FetchAttributesAsync();
                AssertAreEqual(snapshot1.Properties, snapshot1Clone.Properties);

                CloudPageBlob snapshotCopy = container.GetPageBlobReference("blob2");
                await snapshotCopy.StartCopyAsync(TestHelper.Defiddler(snapshot1.Uri));
                await WaitForCopyAsync(snapshotCopy);
                Assert.AreEqual(CopyStatus.Success, snapshotCopy.CopyState.Status);

                await TestHelper.ExpectedExceptionAsync<InvalidOperationException>(
                    async () => await snapshot1.OpenWriteAsync(1024),
                    "Trying to write to a blob snapshot should fail");

                using (Stream snapshotStream = (await snapshot1.OpenReadAsync()))
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                await blob.CreateAsync(1024);

                using (Stream snapshotStream = (await snapshot1.OpenReadAsync()))
                {
                    snapshotStream.Seek(0, SeekOrigin.End);
                    TestHelper.AssertStreamsAreEqual(originalData, snapshotStream);
                }

                BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, null, null, null);
                List<IListBlobItem> blobs = resultSegment.Results.ToList();
                Assert.AreEqual(4, blobs.Count);
                AssertAreEqual(snapshot1, (CloudBlob)blobs[0]);
                AssertAreEqual(snapshot2, (CloudBlob)blobs[1]);
                AssertAreEqual(blob, (CloudBlob)blobs[2]);
                AssertAreEqual(snapshotCopy, (CloudBlob)blobs[3]);
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Create a snapshot with explicit metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobSnapshotMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);

                blob.Metadata["Hello"] = "World";
                blob.Metadata["Marco"] = "Polo";
                await blob.SetMetadataAsync();

                IDictionary<string, string> snapshotMetadata = new Dictionary<string, string>();
                snapshotMetadata["Hello"] = "Dolly";
                snapshotMetadata["Yoyo"] = "Ma";

                CloudPageBlob snapshot = await blob.CreateSnapshotAsync(snapshotMetadata, null, null, null);

                // Test the client view against the expected metadata
                // None of the original metadata should be present
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));

                // Test the server view against the expected metadata
                await snapshot.FetchAttributesAsync();
                Assert.AreEqual("Dolly", snapshot.Metadata["Hello"]);
                Assert.AreEqual("Ma", snapshot.Metadata["Yoyo"]);
                Assert.IsFalse(snapshot.Metadata.ContainsKey("Marco"));
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test conditional access on a blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                await blob.CreateAsync(1024);
                await blob.FetchAttributesAsync();

                string currentETag = blob.Properties.ETag;
                DateTimeOffset currentModifiedTime = blob.Properties.LastModified.Value;

                // ETag conditional tests
                blob.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                await blob.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(currentETag), null, null);

                await blob.FetchAttributesAsync();
                string newETag = blob.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                blob.Metadata["ETagConditionalName"] = "ETagConditionalValue2";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(newETag), null, operationContext),
                    operationContext,
                    "If none match on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                string invalidETag = "\"0x10101010\"";
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfMatchCondition(invalidETag), null, operationContext),
                    operationContext,
                    "Invalid ETag on conditional test should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                currentETag = blob.Properties.ETag;
                await blob.SetMetadataAsync(AccessCondition.GenerateIfNoneMatchCondition(invalidETag), null, null);

                await blob.FetchAttributesAsync();
                newETag = blob.Properties.ETag;

                // LastModifiedTime tests
                currentModifiedTime = blob.Properties.LastModified.Value;

                blob.Metadata["DateConditionalName"] = "DateConditionalValue";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, operationContext),
                    operationContext,
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await blob.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                currentModifiedTime = blob.Properties.LastModified.Value;

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(pastTime), null, operationContext),
                    operationContext,
                    "IfNotModifiedSince conditional on past time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                blob.Metadata["DateConditionalName"] = "DateConditionalValue2";

                currentETag = blob.Properties.ETag;
                await blob.SetMetadataAsync(AccessCondition.GenerateIfNotModifiedSinceCondition(currentModifiedTime), null, null);

                await blob.FetchAttributesAsync();
                newETag = blob.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test page blob methods on a block blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobMethodsOnBlockBlobAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                List<string> blobs = await CreateBlobsAsync(container, 1, BlobType.BlockBlob);
                CloudPageBlob blob = container.GetPageBlobReference(blobs.First());

                OperationContext operationContext = new OperationContext();
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await TestHelper.ExpectedExceptionAsync(
                        async () => await blob.WritePagesAsync(stream, 0, null, null, null, operationContext),
                        operationContext,
                        "Page operations should fail on block blobs",
                        HttpStatusCode.Conflict,
                        "InvalidBlobType");
                }

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.ClearPagesAsync(0, 512, null, null, operationContext),
                    operationContext,
                    "Page operations should fail on block blobs",
                    HttpStatusCode.Conflict,
                    "InvalidBlobType");

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.GetPageRangesAsync(null /* offset */, null /* length */, null, null, operationContext),
                    operationContext,
                    "Page operations should fail on block blobs",
                    HttpStatusCode.Conflict,
                    "InvalidBlobType");
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Test 512-byte page alignment")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobAlignmentAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                CloudPageBlob blob = container.GetPageBlobReference("blob1");
                OperationContext operationContext = new OperationContext();

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.CreateAsync(511, null, null, operationContext),
                    operationContext,
                    "Page operations that are not 512-byte aligned should fail",
                    HttpStatusCode.BadRequest);

                await TestHelper.ExpectedExceptionAsync(
                    async () => await blob.CreateAsync(513, null, null, operationContext),
                    operationContext,
                    "Page operations that are not 512-byte aligned should fail",
                    HttpStatusCode.BadRequest);

                await blob.CreateAsync(512);

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(511);
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(stream, 0, null, null, null, operationContext),
                        "Page operations that are not 512-byte aligned should fail");
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(513);
                    await TestHelper.ExpectedExceptionAsync<ArgumentOutOfRangeException>(
                        async () => await blob.WritePagesAsync(stream, 0, null, null, null, operationContext),
                        "Page operations that are not 512-byte aligned should fail");
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.SetLength(512);
                    await blob.WritePagesAsync(stream, 0, null);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Upload and download null/empty data")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudPageBlobUploadDownloadNoDataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudPageBlob blob = container.GetPageBlobReference("blob");
                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await blob.UploadFromStreamAsync(null),
                    "Uploading from a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await blob.UploadFromStreamAsync(stream);
                }

                await TestHelper.ExpectedExceptionAsync<ArgumentNullException>(
                    async () => await blob.DownloadToStreamAsync(null),
                    "Downloading to a null stream should fail");

                using (MemoryStream stream = new MemoryStream())
                {
                    await blob.DownloadToStreamAsync(stream);
                    Assert.AreEqual(0, stream.Length);
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }
    }
}
