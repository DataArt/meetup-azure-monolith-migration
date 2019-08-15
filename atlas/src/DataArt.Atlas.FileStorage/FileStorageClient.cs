#region License
// =================================================================================================
// Copyright 2018 DataArt, Inc.
// -------------------------------------------------------------------------------------------------
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this work except in compliance with the License.
// You may obtain a copy of the License in the LICENSE file, or at:
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// =================================================================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataArt.Atlas.Infrastructure.Exceptions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Serilog;

namespace DataArt.Atlas.FileStorage
{
    internal class FileStorageClient : IFileStorageClient
    {
        private const string FileNameMetadata = "FileName";

        private readonly CloudStorageAccount cloudStorageAccount;

        public FileStorageClient(FileStorageSettings fileStorageSettings)
        {
            if (string.IsNullOrEmpty(fileStorageSettings?.ConnectionString))
            {
                throw GetConfigurationException();
            }

            try
            {
                cloudStorageAccount = CloudStorageAccount.Parse(fileStorageSettings.ConnectionString);
            }
            catch (Exception ex)
            {
                throw GetConfigurationException(ex);
            }
        }

        public async Task<FileStorageWriteStream> GetWriteStreamAsync(FileType fileType, UploadFileProperties uploadFileProperties)
        {
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var containerReference = blobClient.GetContainerReference(fileType.ToString().ToLower());

            var blobName = GetBlobName(uploadFileProperties.FileName);

            var blobReference = containerReference.GetBlockBlobReference(blobName);
            blobReference.Metadata.Add(FileNameMetadata, uploadFileProperties.FileName);
            blobReference.Properties.ContentType = uploadFileProperties.ContentType;
            blobReference.Properties.ContentDisposition = uploadFileProperties.ContentDisposition;

            Func<Task<FileStorageWriteStream>> openStreamDelegate = async () =>
            {
                var stream = await blobReference.OpenWriteAsync();
                Log.Information($"Write stream to storage was opened for {blobName}");
                return new FileStorageWriteStream { Id = blobName, Stream = stream };
            };

            try
            {
                // try to open stream assuming container exists
                return await openStreamDelegate();
            }
            catch (StorageException e)
            {
                // case when container was not created yet
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    Log.Warning(e, "Exception while opening a write stream to storage");
                    await EnsureContainerExists(containerReference);
                    return await openStreamDelegate();
                }

                Log.Error(e, "Failed to open a write stream to storage");
                throw;
            }
        }

        public async Task<string> UploadAsync(FileType fileType, UploadFileProperties uploadFileProperties, Stream streamToUpload)
        {
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var containerReference = blobClient.GetContainerReference(fileType.ToString().ToLower());

            var blobName = GetBlobName(uploadFileProperties.FileName);

            var blobReference = containerReference.GetBlockBlobReference(blobName);
            blobReference.Metadata.Add(FileNameMetadata, uploadFileProperties.FileName);
            blobReference.Properties.ContentType = uploadFileProperties.ContentType;
            blobReference.Properties.ContentDisposition = uploadFileProperties.ContentDisposition;

            Func<Task<string>> uploadDelegate = async () =>
            {
                await blobReference.UploadFromStreamAsync(streamToUpload);
                Log.Information($"{blobName} was uploaded to storage");
                return blobName;
            };

            try
            {
                // try to upload file assuming container exists
                return await uploadDelegate();
            }
            catch (StorageException e)
            {
                // case when container was not created yet
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    Log.Warning(e, "Exception while uploading file");
                    await EnsureContainerExists(containerReference);
                    return await uploadDelegate();
                }

                Log.Error(e, "Failed to upload a file to storage");
                throw;
            }
        }

        public async Task<FileStorageReadStream> GetReadStreamAsync(FileType fileType, string id)
        {
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var containerReference = blobClient.GetContainerReference(fileType.ToString().ToLower());
            var blobReference = containerReference.GetBlockBlobReference(id);

            try
            {
                var downloadFileStream = await blobReference.OpenReadAsync();

                return new FileStorageReadStream
                {
                    FileName = GetFileName(blobReference),
                    ContentType = blobReference.Properties.ContentType,
                    ContentLength = blobReference.Properties.Length,
                    ContentDisposition = blobReference.Properties.ContentDisposition,
                    Id = id,
                    Stream = downloadFileStream
                };
            }
            catch (Exception e)
            {
                var handledException = HandleStorageException(e);

                if (handledException != null)
                {
                    throw handledException;
                }

                Log.Error(e, $"Failed to open a read stream from storage {id}");

                throw;
            }
        }

        public IEnumerable<string> GetList(FileType fileType)
        {
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var listBlobItems = blobClient.GetContainerReference(fileType.ToString().ToLower()).ListBlobs();

            return listBlobItems.Select(b => Path.GetFileName(b.Uri.LocalPath));
        }

        public async Task DeleteAsync(FileType fileType, string id, bool silent)
        {
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var containerReference = blobClient.GetContainerReference(fileType.ToString().ToLower());
            var blobReference = containerReference.GetBlockBlobReference(id);

            try
            {
                await blobReference.DeleteAsync();
                Log.Information($"{id} was deleted from a storage");
            }
            catch (Exception e)
            {
                var handledException = HandleStorageException(e);

                if (handledException != null)
                {
                    Log.Warning(e, $"Failed to delete {id} from a storage");

                    if (silent)
                    {
                        return;
                    }

                    throw handledException;
                }

                Log.Error(e, $"Failed to delete {id} from a storage");

                if (silent)
                {
                    return;
                }

                throw;
            }
        }

        private static async Task EnsureContainerExists(CloudBlobContainer containerReference)
        {
            try
            {
                await containerReference.CreateIfNotExistsAsync();
                Log.Information($"Storage container {containerReference.Name} was created");
            }
            catch (StorageException ex)
            {
                // case when other thread created container beforehand
                if (ex.RequestInformation.HttpStatusCode != 409)
                {
                    Log.Error(ex, "Failed to create container for uploading file to storage");
                    throw;
                }

                Log.Warning(ex, "Failed to create container for uploading file to storage");
            }
        }

        private string GetFileName(CloudBlockBlob blobReference)
        {
            // as we have old items without metadata
            return blobReference.Metadata.ContainsKey(FileNameMetadata)
                ? blobReference.Metadata[FileNameMetadata]
                : blobReference.Name;
        }

        private static string GetBlobName(string fileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    Path.GetFileName(fileName);
                }
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid file name for upload");
            }

            return $"{Guid.NewGuid().ToString().ToLower()}_{fileName}";
        }

        private static Exception GetConfigurationException(Exception innerException = null)
        {
            return new InvalidOperationException("Invalid configuration of file storage client", innerException);
        }

        private static Exception HandleStorageException(Exception exception)
        {
            var storageException = exception as StorageException;

            if (storageException?.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundException();
            }

            return null;
        }
    }
}
