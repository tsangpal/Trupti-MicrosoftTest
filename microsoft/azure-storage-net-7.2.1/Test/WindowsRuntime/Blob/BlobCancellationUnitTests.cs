// -----------------------------------------------------------------------------------------
// <copyright file="BlobCancellationUnitTests.cs" company="Microsoft">
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#if !NETCORE
using Windows.Foundation;
#endif

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class BlobCancellationUnitTests : BlobTestBase
#if XUNIT
, IDisposable
#endif
    {

#if XUNIT
        // Todo: The simple/nonefficient workaround is to minimize change and support Xunit,
        // removed when we support mstest on projectK
        public BlobCancellationUnitTests()
        {
            MyTestInitialize();
        }
        public void Dispose()
        {
            MyTestCleanup();
        }
#endif

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
        [Description("Cancel blob download to stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobDownloadToStreamCancelAsync()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    await blob.UploadFromStreamAsync(originalBlob);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        OperationContext operationContext = new OperationContext();
                        var tokenSource = new CancellationTokenSource();
                        Task action = blob.DownloadToStreamAsync(downloadedBlob, null, null, operationContext, tokenSource.Token);
                        await Task.Delay(100);
                        tokenSource.Cancel();
                        try
                        {
                            await action;
                        }
                        catch (Exception)
                        {
                            Assert.AreEqual(operationContext.LastResult.Exception.Message, "A task was canceled.");
                            Assert.AreEqual(operationContext.LastResult.HttpStatusCode, 306);
                            //Assert.AreEqual(operationContext.LastResult.HttpStatusMessage, "Unused");
                        }
                        TestHelper.AssertNAttempts(operationContext, 1);
                    }
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().Wait();
            }
        }

        [TestMethod]
        [Description("Cancel upload from stream")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlockBlobUploadFromStreamCancelAsync()
        {
            byte[] buffer = GetRandomBuffer(1 * 1024 * 1024);
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");
                using (MemoryStream originalBlob = new MemoryStream(buffer))
                {
                    using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                    {
                        OperationContext operationContext = new OperationContext();
                        var tokenSource = new CancellationTokenSource();
                        Task action = blob.UploadFromStreamAsync(originalBlob, originalBlob.Length, null, null, operationContext, tokenSource.Token);
                        await Task.Delay(1000); //we need a bit longer time in order to put the cancel output exception to operationContext.LastResult
                        tokenSource.Cancel();
                        try
                        {
                            await action;
                        }
                        catch (Exception)
                        {
                            Assert.AreEqual(operationContext.LastResult.Exception.Message, "A task was canceled.");
                            Assert.AreEqual(operationContext.LastResult.HttpStatusCode, 306);
                            //Assert.AreEqual(operationContext.LastResult.HttpStatusMessage, "Unused");
                        }
                        TestHelper.AssertNAttempts(operationContext, 1);
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
