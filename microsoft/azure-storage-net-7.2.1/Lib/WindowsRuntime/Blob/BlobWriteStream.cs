// -----------------------------------------------------------------------------------------
// <copyright file="BlobWriteStream.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class BlobWriteStream : BlobWriteStreamBase
    {
        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for a block blob.
        /// </summary>
        /// <param name="blockBlob">Blob reference to write to.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        internal BlobWriteStream(CloudBlockBlob blockBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
            : base(blockBlob, accessCondition, options, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for a page blob.
        /// </summary>
        /// <param name="pageBlob">Blob reference to write to.</param>
        /// <param name="pageBlobSize">Size of the page blob.</param>
        /// <param name="createNew">Use <c>true</c> if the page blob is newly created, <c>false</c> otherwise.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>        
        internal BlobWriteStream(CloudPageBlob pageBlob, long pageBlobSize, bool createNew, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
            : base(pageBlob, pageBlobSize, createNew, accessCondition, options, operationContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BlobWriteStream class for an append blob.
        /// </summary>
        /// <param name="appendBlob">Blob reference to write to.</param>
        /// <param name="accessCondition">An object that represents the access conditions for the blob. If null, no condition is used.</param>
        /// <param name="options">An object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>        
        internal BlobWriteStream(CloudAppendBlob appendBlob, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
            : base(appendBlob, accessCondition, options, operationContext)
        {
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <c>SeekOrigin</c> indicating the reference
        /// point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long oldOffset = this.currentOffset;
            long newOffset = this.GetNewOffset(offset, origin);

            if (oldOffset != newOffset)
            {
                if (this.blobMD5 != null)
                {
                    this.blobMD5.Dispose();
                    this.blobMD5 = null;
                }

                this.Flush();
            }

            this.currentOffset = newOffset;
            this.currentBlobOffset = newOffset;
            return this.currentOffset;
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current
        /// position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CommonUtility.RunWithoutSynchronizationContext(() => this.WriteAsync(buffer, offset, count).Wait());
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the current stream, advances the current
        /// position within this stream by the number of bytes written, and monitors cancellation requests.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from
        /// buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("buffer", buffer);
            CommonUtility.AssertInBounds("offset", offset, 0, buffer.Length);
            CommonUtility.AssertInBounds("count", count, 0, buffer.Length - offset);

            if (this.lastException != null)
            {
                throw this.lastException;
            }

            if (this.committed)
            {
                throw new InvalidOperationException(SR.BlobStreamAlreadyCommitted);
            }

            if (this.blobMD5 != null)
            {
                this.blobMD5.UpdateHash(buffer, offset, count);
            }

            this.currentOffset += count;
            while (count > 0)
            {
                int maxBytesToWrite = this.streamWriteSizeInBytes - (int)this.internalBuffer.Length;
                int bytesToWrite = Math.Min(count, maxBytesToWrite);

                this.internalBuffer.Write(buffer, offset, bytesToWrite);
                if (this.blockMD5 != null)
                {
                    this.blockMD5.UpdateHash(buffer, offset, bytesToWrite);
                }

                count -= bytesToWrite;
                offset += bytesToWrite;

                if (bytesToWrite == maxBytesToWrite)
                {
                    await this.DispatchWriteAsync();
                }
            }
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying blob.
        /// </summary>
        public override void Flush()
        {
            CommonUtility.RunWithoutSynchronizationContext(() => this.FlushAsync().Wait());
        }

        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (this.lastException != null)
            {
                throw this.lastException;
            }

            if (this.committed)
            {
                throw new InvalidOperationException(SR.BlobStreamAlreadyCommitted);
            }

            await this.DispatchWriteAsync();
            await Task.Run(() => this.noPendingWritesEvent.Wait(), cancellationToken);

            if (this.lastException != null)
            {
                throw this.lastException;
            }
        }

        /// <summary>
        /// Releases the blob resources used by the Stream.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;

                if (disposing)
                {
                    if (!this.committed)
                    {
                        CommonUtility.RunWithoutSynchronizationContext(() => this.CommitAsync().Wait());
                    }
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying blob, and commits the blob.
        /// </summary>
        /// <returns>A task that represents the asynchronous commit operation.</returns>
        public override async Task CommitAsync()
        {
            await this.FlushAsync();
            this.committed = true;

            try
            {
                if (this.blockBlob != null)
                {
                    if (this.blobMD5 != null)
                    {
                        this.blockBlob.Properties.ContentMD5 = this.blobMD5.ComputeHash();
                    }

                    await this.blockBlob.PutBlockListAsync(this.blockList, this.accessCondition, this.options, this.operationContext);
                }
                else
                {
                    if (this.blobMD5 != null)
                    {
                        this.Blob.Properties.ContentMD5 = this.blobMD5.ComputeHash();
                        await this.Blob.SetPropertiesAsync(this.accessCondition, this.options, this.operationContext);
                    }
                }
            }
            catch (Exception e)
            {
                this.lastException = e;
                throw;
            }
        }

        /// <summary>
        /// Asynchronously dispatches a write operation.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task DispatchWriteAsync()
        {
            if (this.internalBuffer.Length == 0)
            {
                return;
            }

            MultiBufferMemoryStream bufferToUpload = this.internalBuffer;
            this.internalBuffer = new MultiBufferMemoryStream(this.Blob.ServiceClient.BufferManager);
            bufferToUpload.Seek(0, SeekOrigin.Begin);

            string bufferMD5 = null;
            if (this.blockMD5 != null)
            {
                bufferMD5 = this.blockMD5.ComputeHash();
                this.blockMD5.Dispose();
                this.blockMD5 = new MD5Wrapper();
            }

            if (this.blockBlob != null)
            {
                string blockId = this.GetCurrentBlockId();
                this.blockList.Add(blockId);
                await this.WriteBlockAsync(bufferToUpload, blockId, bufferMD5);
            }
            else if (this.pageBlob != null)
            {
                if ((bufferToUpload.Length % Constants.PageSize) != 0)
                {
                    this.lastException = new IOException(SR.InvalidPageSize);
                    throw this.lastException;
                }

                long offset = this.currentBlobOffset;
                this.currentBlobOffset += bufferToUpload.Length;
                await this.WritePagesAsync(bufferToUpload, offset, bufferMD5);
            }
            else
            {
                long offset = this.currentBlobOffset;
                this.currentBlobOffset += bufferToUpload.Length;

                // We cannot differentiate between max size condition failing only in the retry versus failing in the 
                // first attempt and retry even for a single writer scenario. So we will eliminate the latter and handle 
                // the former in the append operation call.
                if (this.accessCondition.IfMaxSizeLessThanOrEqual.HasValue && this.currentBlobOffset > this.accessCondition.IfMaxSizeLessThanOrEqual.Value)
                {
                    this.lastException = new IOException(SR.InvalidBlockSize);
                    throw this.lastException;
                }

                await this.WriteAppendBlockAsync(bufferToUpload, offset, bufferMD5);
            }
        }

        /// <summary>
        /// Starts an asynchronous PutBlock operation as soon as the parallel
        /// operation semaphore becomes available.
        /// </summary>
        /// <param name="blockData">Data to be uploaded</param>
        /// <param name="blockId">Block ID</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteBlockAsync(Stream blockData, string blockId, string blockMD5)
        {
            this.noPendingWritesEvent.Increment();
            await this.parallelOperationSemaphore.WaitAsync();
            Task putBlockTask = this.blockBlob.PutBlockAsync(blockId, blockData, blockMD5, this.accessCondition, this.options, this.operationContext).ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    this.lastException = task.Exception;
                }

                this.noPendingWritesEvent.Decrement();
                this.parallelOperationSemaphore.Release();
            });
        }

        /// <summary>
        /// Starts an asynchronous WritePages operation as soon as the parallel
        /// operation semaphore becomes available.
        /// </summary>
        /// <param name="pageData">Data to be uploaded</param>
        /// <param name="offset">Offset within the page blob</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WritePagesAsync(Stream pageData, long offset, string contentMD5)
        {
            this.noPendingWritesEvent.Increment();
            await this.parallelOperationSemaphore.WaitAsync();
            Task writePagesTask = this.pageBlob.WritePagesAsync(pageData, offset, contentMD5, this.accessCondition, this.options, this.operationContext).ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    this.lastException = task.Exception;
                }

                this.noPendingWritesEvent.Decrement();
                this.parallelOperationSemaphore.Release();
            });
        }

        /// <summary>
        /// Starts an asynchronous AppendBlock operation as soon as the parallel
        /// operation semaphore becomes available. Since parallelism is always set
        /// to 1 for append blobs, appendblock operations are called serially.
        /// </summary>
        /// <param name="blockData">Data to be uploaded</param>
        /// <param name="offset">Offset within the append blob to be used to set the append offset conditional header.</param>        
        /// <param name="blockMD5">MD5 hash of the data to be uploaded</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteAppendBlockAsync(Stream blockData, long offset, string blockMD5)
        {
            this.noPendingWritesEvent.Increment();
            await this.parallelOperationSemaphore.WaitAsync();

            this.accessCondition.IfAppendPositionEqual = offset;

            int previousResultsCount = this.operationContext.RequestResults.Count;
            Task writeBlockTask = this.appendBlob.AppendBlockAsync(blockData, blockMD5, this.accessCondition, this.options, this.operationContext).ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    if (this.options.AbsorbConditionalErrorsOnRetry.Value
                        && this.operationContext.LastResult.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                    {
                        StorageExtendedErrorInformation extendedInfo = this.operationContext.LastResult.ExtendedErrorInformation;
                        if (extendedInfo != null 
                            && (extendedInfo.ErrorCode == BlobErrorCodeStrings.InvalidAppendCondition || extendedInfo.ErrorCode == BlobErrorCodeStrings.InvalidMaxBlobSizeCondition)
                            && (this.operationContext.RequestResults.Count - previousResultsCount > 1))
                        {
                            // Pre-condition failure on a retry should be ignored in a single writer scenario since the request
                            // succeeded in the first attempt.
                            Logger.LogWarning(this.operationContext, SR.PreconditionFailureIgnored);
                        }
                        else
                        {
                            this.lastException = task.Exception;
                        }
                    }
                    else
                    {
                        this.lastException = task.Exception;
                    }
                }

                this.noPendingWritesEvent.Decrement();
                this.parallelOperationSemaphore.Release();
            });
        }
    }
}