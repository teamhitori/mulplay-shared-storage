using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using TeamHitori.Mulplay.shared.storage.documents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.shared.storage
{
    public static class StorageExtensions
    {

        static string _version = Environment.GetEnvironmentVariable("Assembly_Version");

        /// <summary>
        /// Returns deserialised Object from json string input using JsonConvert
        /// </summary>
        /// <typeparam name="T">The type to serialize input to</typeparam>
        /// <param name="json">input json string</param>
        /// <returns>Deserialized Object of type T</returns>
        public static T GetObject<T>(this String json) where T : class
        {
            if (json == null) return null;

            var type = typeof(T);
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns deserialised Object from JDoc input using JsonConvert
        /// </summary>
        /// <typeparam name="T">The type to serialize input to</typeparam>
        /// <param name="doc">input JDoc object</param>
        /// <returns>Deserialized Object of type T</returns>
        public static T GetObject<T>(this JDoc doc) where T : class
        {
            if (doc == null) return null;

            return doc.content.GetObject<T>();
        }

        /// <summary>
        /// Returns deserialised Object from UserDocument input using JsonConvert
        /// </summary>
        /// <typeparam name="T">The type to serialize input to</typeparam>
        /// <param name="doc">input UserDocument object</param>
        /// <returns>Deserialized Object of type T</returns>
        public static T GetObject<T>(this UserDocument doc) where T : class
        {
            if (doc == null) return null;

            return doc.content.GetObject<T>();
        }

        /// <summary>
        /// Returns deserialised Object from UserDocumentGen<T> input using JsonConvert
        /// </summary>
        /// <typeparam name="T">The type to serialize input to</typeparam>
        /// <param name="doc">input UserDocument object</param>
        /// <returns>Deserialized Object of type T</returns>
        public static T GetObject<T>(this UserDocumentGen<T> doc) where T : class
        {
            if (doc == null) return null;

            return doc.contents.GetObject<T>();
        }


        /// <summary>
        /// Returns serialized JDoc object from T input
        /// </summary>
        /// <typeparam name="T">The Object type to serialize</typeparam>
        /// <param name="obj">The Object to serialize</param>
        /// <returns>Serialized JDoc object</returns>
        public static JDoc ToJDoc<T>(this T obj)
        {
            var fqn = typeof(T).FullName;
            var content = JsonConvert.SerializeObject(obj);
            return new JDoc(fqn, content);
        }

        /// <summary>
        /// Create Typed Singleton UserDocumentGen from storage
        /// </summary>
        /// <typeparam name="T">Type of object in UserDocumentGen</typeparam>
        /// <param name="storage">storage object</param>
        /// <param name="obj">object to be serialized into UserDocument</param>
        /// <param name="createSingletonDoc">true if should be created as singleton obj, using "Singleton" primaryName</param>
        /// <param name="primaryNameIN">optional, primary name to use as override</param>
        /// <returns>new UserDocumentGen using obj in</returns>
        public static UserDocumentGen<T> CreateSingleton<T>(
            this Storage storage,
            T obj)
        {

            return storage.CreateUserDocument(obj, true);
        }

        /// <summary>
        /// Create Typed UserDocumentGen from storage
        /// </summary>
        /// <typeparam name="T">Type of object in UserDocumentGen</typeparam>
        /// <param name="storage">storage object</param>
        /// <param name="obj">object to be serialized into UserDocument</param>
        /// <param name="createSingletonDoc">true if should be created as singleton obj, using "Singleton" primaryName</param>
        /// <param name="primaryNameIN">optional, primary name to use as override</param>
        /// <returns>new UserDocumentGen using obj in</returns>
        public static UserDocumentGen<T> CreateUserDocument<T>(
            this Storage storage,
            T obj,
            bool createSingletonDoc = false,
            String primaryNameIN = null)
        {
            var altPrimaryName = System.Guid.NewGuid().ToString();
            if (createSingletonDoc)
            {
                altPrimaryName = "Singleton";
            }
            var primaryName = primaryNameIN ?? altPrimaryName;

            //val cal = Calendar.getInstance()
            //val sdf = SimpleDateFormat("yyyy/MM/dd HH:mm:ssZ")
            var dateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ssZ");

            var jDoc = obj.ToJDoc();
            var appOwner = "";  // this is not enforced yet
            var autoId = Guid.NewGuid().ToString();

            var userDoc = new UserDocumentGen<T>(autoId, appOwner, storage.UserId, primaryName, jDoc.content, 0, dateTime, null);

            return userDoc;
        }

        /// <summary>
        /// Update or insert new document baed on primary name and type
        /// </summary>
        /// <typeparam name="T">Type of object in UserDocumentGen</typeparam>
        /// <param name="storage">storage object</param>
        /// <param name="obj">object to be serialized into UserDocument</param>
        /// <param name="createSingletonDoc">true if should be created as singleton obj, using "Singleton" primaryName</param>
        /// <param name="primaryNameIN">optional, primary name to use as override</param>
        /// <returns></returns>
        public static async Task<UserDocumentGen<T>> Upsert<T>(this Storage storage,
            T obj,
            bool createSingletonDoc = false,
            String primaryNameIN = null) where T : class
        {
            var document = storage.CreateUserDocument(obj, createSingletonDoc, primaryNameIN);

            return await storage.Upsert(document);
        }


            /// <summary>
            /// Update or insert new document baed on primary name and type
            /// </summary>
            /// <typeparam name="T">Type of object in UserDocumentGen</typeparam>
            /// <param name="storage">storage object</param>
            /// <param name="document">Generic Document wapper of serialized type T to upsert</param>
            /// <returns></returns>
            public static async Task<UserDocumentGen<T>> Upsert<T>(this Storage storage, UserDocumentGen<T> document) where T : class
        {
            var fqn = typeof(T).FullName;
            var latestDoc = await storage.FindDocumentByPrimaryName<T>(document.primaryName);

            var dateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ssZ");
            var autoId = Guid.NewGuid().ToString();
            var inDoc = new UserDocument(
                autoId,
                document.appOwner,
                document.userPrincipleId,
                document.primaryName.ToLower(),
                fqn,
                document.contents,
                (latestDoc?.version ?? 0) + 1,
                dateTime,
                latestDoc?.etag);

            try
            {

                storage.LogInformation($"Upserting [{fqn}] primaryName: {inDoc.primaryName}, version: {inDoc.version}, " +
               $"id: {inDoc.id}, userPriciple: {inDoc.userPrincipleId}");

                storage.Cache?.KeyDelete(document.GetPrimaryKey());
                storage.Cache?.KeyDelete($"[{storage.UserId}][findAllByType][{fqn}]");

                var res = await storage.Repository.CreateItemAsync(inDoc);

                var newDoc = await storage.FindDocumentByPrimaryName<T>(document.primaryName);

                return newDoc;

            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {

                }

                storage.LogInformation($"{ex.Message} [{fqn}] primaryName: {inDoc.primaryName}, version: {inDoc.version}, " +
                        $"id: {inDoc.id}, userPriciple: {inDoc.userPrincipleId}, _etag:{inDoc._etag}", ex);

                var newDoc = await storage.FindDocumentByPrimaryName<T>(document.primaryName);

                storage.LogInformation($"Conflict resolved item [{fqn}] primaryName: {newDoc?.primaryName}, version: {newDoc?.version}, " +
                    $"id: {newDoc?.id}, userPriciple: {newDoc?.userPrincipleId}, _etag:{newDoc?.etag}");

                return newDoc;
            }
            catch (Exception ex)
            {
                storage.LogError($"Encountered exception {ex.Message} [{fqn}] {document?.primaryName}", ex);
                throw;
            }

        }

        /// <summary>
        /// Delete User Document by primaryName
        /// </summary>
        /// <param name="storage">storage object</param>
        /// <param name="primaryName">primaryName of Document to delete</param>
        /// <returns>string</returns>
        public static async Task<bool> DeleteDocument<T>(this Storage storage, UserDocumentGen<T> document) where T : class
        {
            var latestDoc = await storage.FindDocumentByPrimaryName<T>(document.primaryName);

            if (latestDoc != null)
            {
                var delDoc = storage.CreateUserDocument<T>(null, false, document.primaryName);

                await storage.Upsert(delDoc);

                return true;
            }

            return false;
        }


        /// <summary>
        /// Delete User Document by Id
        /// </summary>
        /// <param name="storage">storage object</param>
        /// <param name="docId">docId of Document to delete</param>
        /// <returns>string</returns>
        public static async Task<string> DeleteDocumentById(this Storage storage, string docId)
        {
            var res = await storage.Repository.ExecuteSproc<string>(
                               "deleteDocumentById",
                               storage.UserId,
                               new[] {
                                    docId
                               });

            return res;
        }




        /// <summary>
        /// Query document storage for all documents that match either T or fdnOverride
        /// </summary>
        /// <typeparam name="T">Type of object in UserDocumentGen</typeparam>
        /// <param name="storage">storage object</param>
        /// <param name="fqnOverride">Fully qualified name override if object stored under type other than fqn of T</param>
        /// <returns>List of matching Generic Documents</returns>
        public static async Task<IEnumerable<UserDocumentGen<T>>> FindAllByType<T>(this Storage storage, String fqnOverride = null) where T : class
        {
            var fqn = fqnOverride ?? typeof(T).FullName;

            var finalRes = await storage.GetCacheOrElse($"[{storage.UserId}][findAllByType][{fqn}]", async () =>
            {
                var liveDocs = await storage.Repository.ExecuteSproc<List<UserDocument>>(
                           "findAllByType",
                           storage.UserId,
                           new[] {
                    fqn,
                    storage.UserId
                           });

                if (liveDocs == null)
                {
                    return new List<UserDocumentGen<T>>();
                }

                var result = liveDocs
                   .Where(x => x.content.GetObject<T>() != null)
                   .Select(x => x.GetUserDocument<T>());

                storage.LogInformation($"Found ({result.Count()}) items matching type ({ typeof(T)})");

                return result;
            });

            return finalRes;
        }

        /// <summary>
        /// Retrieve document storage by singleton name and type T
        /// </summary>
        /// <typeparam name="T">Type of object in UserDocumentGen</typeparam>
        /// <param name="storage">storage object</param>
        /// <param name="primaryName"></param>
        /// <returns>List of matching Generic Documents</returns>
        public static async Task<UserDocumentGen<T>> GetSingleton<T>(this Storage storage) where T : class
        {

            return await storage.FindDocumentByPrimaryName<T>("Singleton");
        }

        /// <summary>
        /// Search document storage by primary name and type T
        /// </summary>
        /// <typeparam name="T">Type of object in UserDocumentGen</typeparam>
        /// <param name="storage">storage object</param>
        /// <param name="primaryName"></param>
        /// <returns>List of matching Generic Documents</returns>
        public static async Task<UserDocumentGen<T>> FindDocumentByPrimaryName<T>(this Storage storage, String primaryName) where T : class
        {
            var fqn = typeof(T).FullName;

            primaryName = primaryName.ToLower();

            var finalRes = await storage.GetCacheOrElse($"[{storage.UserId}][{primaryName}][{fqn}]", async () =>
            {
                

                var docsString = await storage.Repository.ExecuteSproc<String>(
                                "findDocumentByPrimaryName",
                                storage.UserId,
                                new[] {
                                    primaryName,
                                    fqn,
                                    storage.UserId
                                });

                var wrappedJson = $"{{\"docs\":{docsString}}}";
                var wrapper = wrappedJson.GetObject<DocWrapper>();

                if (wrapper == null || wrapper.docs == null)
                {
                    storage.LogInformation($"Found no matching doc [{fqn}] primaryName: {primaryName}");
                    return null;
                }

                var result = wrapper
                    .docs
                    .FirstOrDefault();

                if (result != null)
                {
                    if (result.content.GetObject<T>() != null)
                    {
                        storage.LogInformation($"Found matching doc [{result.type}] primaryName: {primaryName} version: {result.version}, " +
                            $"id: {result.id}, userPriciple: {result.userPrincipleId}");
                    }
                    else
                    {
                        storage.LogInformation($"Found null doc [{result.type}] primaryName: {primaryName} version: {result.version}, " +
                            $"id: {result.id}, userPriciple: {result.userPrincipleId}");
                    }
                }
                else
                {
                    storage.LogInformation($"Found no matching doc primaryName: {primaryName} [{fqn}]");
                }

                return result?.GetUserDocument<T>();

            });

            return finalRes;
        }

        public async static Task<T> GetCacheOrElse<T>(this Storage storage, string primaryKey, Func<Task<T>> altFunc) where T : class
        {
            T result = null;
            var fqn = typeof(T).FullName;

            try
            {
                
                var resultVal = storage.Cache?.StringGet(primaryKey);
                if (resultVal?.HasValue == true)
                {
                    var resultStr = resultVal.ToString();

                    if(resultStr != null)
                    {
                        storage.LogInformation($"Retrieved from cache for primaryKey: {primaryKey} [{fqn}] ");
                    }

                    result = resultStr.GetObject<T>();
                }
            }
            catch (Exception e)
            {

                storage.LogError($"Error occured retrieving RedisCache: {primaryKey} [{fqn}]", e);
            }

            if (result == null)
            {
                try
                {
                    var newRes = await altFunc();

                    if (newRes != null)
                    {
                        var contents = newRes.ToJDoc().content;
                        storage.Cache?.StringSet(primaryKey, contents);
                        storage.LogInformation($"Added to cache for primaryKey: {primaryKey} [{fqn}]");
                    }

                    return newRes;

                } catch (Exception e)
                {
                    storage.LogError($"Error occured retrieving Cosmosdb: {primaryKey} [{fqn}]", e);
                    return result;
                }
                

            }
            else
            {
                return result;
            }
        }

        public static async Task<Tuple<Image, IImageFormat>> GetImageFromHashLessThan(this Storage storage, string hashNameRaw)
        {

            var image = await storage.GetImageFromBlob(hashNameRaw);

            return image;
        }

        public static async Task<Tuple<Image, IImageFormat>> GetImageFromBlob(this Storage storage, string id)
        {
            var idFormatted = id.Replace(" ", "");

            var blob = storage.BlobContainerClient.GetBlobClient(idFormatted);

            MemoryStream ms = new MemoryStream();

            var res = await blob.DownloadAsync();

            res.Value.Content.CopyTo(ms);

            var array = ms.ToArray();

            var image = Image.Load(array, out IImageFormat format);

            return Tuple.Create(image, format);
        }

        public static async Task<string> SaveImage(this Storage storage, ImageBinaryString imageBinaryString, string hashName = null)
        {
            var imgStr = imageBinaryString.imageStr;

            byte[] imgByte = Convert.FromBase64String(imgStr);

            MemoryStream ms = new MemoryStream(imgByte);

            var image = Image.Load(ms, out IImageFormat format).CloneAs<Rgba32>();

            return await storage.SaveImage(image, format, hashName);

        }

        public static async Task<string> SaveImage(this Storage storage, Image<Rgba32> image, IImageFormat format, string hashName = null)
        {

            if (string.IsNullOrEmpty(hashName))
            {
                var data = image.GetByteArray();
                var md5 = System.Security.Cryptography.MD5.Create();
                var hashBytes = md5.ComputeHash(data.ToArray());

                hashName = BitConverter.ToString(hashBytes);
                // hashName = Convert.ToBase64String(hashBytes);
            }


            storage.LogInformation($"SaveImage: {hashName}");


            // upsert blob
            var blockBlob = storage.BlobContainerClient.GetBlobClient(hashName);

            var exists = await blockBlob.ExistsAsync();

            if (exists)
            {
                storage.LogInformation($"blob {hashName} already exists, deleting");
                await blockBlob.DeleteAsync();
            }

            using (var msWrite = new MemoryStream())
            {
                storage.LogInformation($"Uploading blob {hashName}");

                var contentType = "image/bmp";

                var @switch = new Dictionary<Type, Action<IImageFormat>> {
                        { typeof(BmpFormat), f => {
                            var encoder = new BmpEncoder(){
                                BitsPerPixel = BmpBitsPerPixel.Pixel8
                            };

                            image.SaveAsBmp(msWrite, encoder);

                            contentType = "image/bmp";
                        } },
                        { typeof(PngFormat), f => {
                        var encoder = new PngEncoder(){
                                CompressionLevel = PngCompressionLevel.BestCompression,
                                BitDepth = PngBitDepth.Bit8
                            };

                            image.SaveAsPng(msWrite, encoder);

                            contentType = "image/png";
                        }},
                        { typeof(JpegFormat), f => {
                        var encoder = new JpegEncoder(){
                                Quality = 50
                            };

                            image.SaveAsJpeg(msWrite, encoder);

                            contentType = "image/jpeg";
                        } },
                        { typeof(GifFormat),f => {
                        var encoder = new GifEncoder(){

                            };

                            image.SaveAsGif(msWrite, encoder);

                            contentType = "image/gif";
                        } }
                    };

                @switch[format.GetType()](format);

                msWrite.Position = 0;

                await blockBlob.UploadAsync(msWrite, new BlobHttpHeaders { ContentType = contentType });
            }

            return hashName;
        }

        // need to make more robust
        public static async Task<string> CreatePrimaryName<T>(this Storage storage) where T : class
        {
            for (int i = 0; i < 10; i++)
            {
                var primaryName = RandomString(6);
                var targetStorage = new Storage(storage.Repository, storage.BlobServiceClient, storage.StorageSharedKeyCredential, primaryName, storage.Logger);
                var existingDef = await targetStorage.GetSingleton<T>();

                if (existingDef == null)
                {
                    storage.LogInformation($"Created {typeof(T).Name} def with no collisions in {i} attempts");
                    return primaryName;
                }
            }
            storage.LogInformation($"ran out of attempts to create new live storage name, exiting");
            throw new InvalidDataException($"ran out of attempts to create new live storage name, exiting");

        }

        public static Guid CreateCryptographicallySecureGuid()
        {
            using (var provider = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[16];
                provider.GetBytes(bytes);

                return new Guid(bytes);
            }
        }

        private const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        private static Random random = new Random();
        private static string RandomString(int length)
        {

            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static bool IsValidId(this string id)
        {
            return !id.Any(letter => !chars.Contains(letter));
        }

        public static byte[] GetByteArray(this Image<Rgba32> imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.SaveAsBmp(ms);
                return ms.ToArray();
            }
        }

        public static async Task revokeSasPolicy(this Storage storage, string forUserId = null)
        {
            var res = await storage.BlobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                        DateTimeOffset.UtcNow.AddSeconds(7));

        }

        public static string GetSasToken(this Storage storage, BlobSasPermissions blobSasPermissions, string autoDelete = "Day1")
        {
            // Get a user delegation key for the Blob service that's valid for seven days.
            // You can use the key to generate any number of shared access signatures over the lifetime of the key.
            //UserDelegationKey key = await storage.BlobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));

            var sharedAccessExpiryTime = DateTime.UtcNow.AddDays(7);
            switch (autoDelete)
            {
                case "Hours12":
                    sharedAccessExpiryTime = DateTime.UtcNow.AddHours(12);
                    break;
                case "Day1":
                    sharedAccessExpiryTime = DateTime.UtcNow.AddDays(1);
                    break;
                case "Week1":
                    sharedAccessExpiryTime = DateTime.UtcNow.AddDays(7);
                    break;
                case "Week2":
                    sharedAccessExpiryTime = DateTime.UtcNow.AddDays(14);
                    break;
                case "Week4":
                    sharedAccessExpiryTime = DateTime.UtcNow.AddDays(28);
                    break;
            }

            // Create a SAS token that's valid until sharedAccessExpiryTime
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = storage.BlobContainerClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = sharedAccessExpiryTime
            };

            // Specify read permissions for the SAS.
            sasBuilder.SetPermissions(blobSasPermissions);

            // Use the key to get the SAS token.
            string sasToken = sasBuilder.ToSasQueryParameters(storage.StorageSharedKeyCredential).ToString();

            return $"?{sasToken}";
        }


        public static void LogWarning(this Storage storage, string message, params object[] args)
        {
            storage.Logger.LogWarning($"[{_version}][{storage.UserId}] {message}", args);
        }

        public static void LogInformation(this Storage storage, string message, params object[] args)
        {
            storage.Logger.LogInformation($"[{_version}][{storage.UserId}] {message}", args);
        }

        public static void LogDebug(this Storage storage, string message, params object[] args)
        {
            storage.Logger.LogDebug($"[{_version}][{storage.UserId}] {message}", args);
        }

        public static void LogError(this Storage storage, string message, params object[] args)
        {
            storage.Logger.LogError($"[{_version}][{storage.UserId}] {message}", args);
        }

        public static void LogError(this Storage storage, Exception exception, string message, params object[] args)
        {
            storage.Logger.LogError(exception, $"[{_version}][{storage.UserId}] {message}", args);
        }


        public static void LogInformation(this Storage storage, string message, Exception ex = null)
        {
            storage.Logger.LogInformation($"[{ _version}][{ storage.UserId}] { message}", ex);
        }

        public static void LogError(this Storage storage, string message, Exception ex = null)
        {
            storage.Logger.LogError($"[{_version}][{storage.UserId}] {message}", ex);
        }

        private class MyBlobProperties
        {
            public long ContentLength { get; set; }
        }
    }
}
