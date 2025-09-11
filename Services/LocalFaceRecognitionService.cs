using fblib;
using SkiaSharp;
using System.Data.Common;
using System.Diagnostics;

namespace fbapp.Services
{
    public class LocalFaceRecognizeService
    {
        private readonly FaceDataService _databaseService;
        private readonly FaceRecDataService _databaseRecService;
        private readonly FaceRecognition _faceRecognition;

        public LocalFaceRecognizeService(
            FaceDataService databaseService,
            FaceRecDataService databaseRecService,
            FaceRecognition faceRecognition)
        {
            _databaseService = databaseService;
            _databaseRecService = databaseRecService;
            _faceRecognition = faceRecognition;
        }

        #region --- Public Methods (Replicating Controller Actions) ---

        public async Task<string> Recognize(Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
            {
                throw new ArgumentException("No image file provided.");
            }

            var filePath = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString() + ".jpg");
            try
            {
                await CompressAndCopyImageBeforeRecognizeAsync(imageStream, filePath);

                string result = "Uploaded image is not recognized";
                string? getMatchedFace = await _faceRecognition.MatchedFaceAsync(filePath);

                if (getMatchedFace == "Spoof detected")
                {
                    return "Spoof detected";
                }

                if (!string.IsNullOrEmpty(getMatchedFace))
                {
                    result = getMatchedFace;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}, Error recognizing face from uploaded image");
                throw new Exception("Error recognizing face from uploaded image", ex);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        public async Task<string> Register(string biometricId, string name, Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
            {
                throw new ArgumentException("No image file provided.");
            }

            try
            {
                var faceId = 0;
                var isBioIdRegistered = _databaseService.IsBioIdRegistered(biometricId, out faceId);

                var faceRecord = new FaceRecord
                {
                    Id = faceId,
                    BiometricId = biometricId,
                    Name = name,
                    ImagePath = Path.Combine(FileSystem.AppDataDirectory, "Registered", biometricId + ".jpg")
                };

                var registeredImagesDir = Path.GetDirectoryName(faceRecord.ImagePath);
                if (!Directory.Exists(registeredImagesDir))
                {
                    Directory.CreateDirectory(registeredImagesDir);
                }

                if (isBioIdRegistered)
                {
                    _databaseService.UpdateFace(faceRecord);
                }
                else
                {
                    _databaseService.InsertFace(faceRecord);
                }

                await CompressAndCopyImageBeforeRegisterAsync(imageStream, faceRecord.ImagePath);
                _faceRecognition.InsertUpdateFaceToDatabase(faceRecord.ImagePath, faceRecord);

                return "Registration successful";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}, Error registering biometric data");
                throw new Exception("Error registering biometric data", ex);
            }
        }

        public async Task RecalculateEmbeddings()
        {
            await _faceRecognition.RecalculateEmbeddings();
        }

        public List<FaceRecord> GetAllRegistered()
        {
            return _databaseService.GetFaceRecords();
        }

        public string? GetBioImage(string bioId)
        {
            var facePath = _databaseService.GetFacePath(bioId);
            if (facePath != null && File.Exists(facePath))
            {
                return facePath;
            }
            return null;          
        }

        public async Task DeleteBio(string bioId)
        {
            try
            {
                var facePath = _databaseService.GetFacePath(bioId);

                if (!string.IsNullOrEmpty(facePath) && File.Exists(facePath))
                {
                    File.Delete(facePath);
                }

                _databaseService.DeleteBio(bioId);
                _databaseRecService.DeleteEmbedding(bioId);

                await _faceRecognition.LoadFacesFromDatabaseAsync();
            }
            catch (DbException ex)
            {
                throw new Exception("Error deleting bio", ex);
            }
        }

        #endregion

        #region --- Private Helper Methods (Copied from Controller) ---

        private static async Task CompressAndCopyImageBeforeRecognizeAsync(Stream imageStream, string filePath)
        {
            await CompressAndSaveImageAsync(imageStream, filePath);
        }

        private static async Task CompressAndCopyImageBeforeRegisterAsync(Stream imageStream, string filePath)
        {
            await CompressAndSaveImageAsync(imageStream, filePath);
        }

        private static async Task CompressAndSaveImageAsync(Stream inputStream, string filePath, int maxWidth = 800, int maxHeight = 600)
        {
            Stream streamToDecode;
            if (!inputStream.CanSeek)
            {
                var ms = new MemoryStream();
                await inputStream.CopyToAsync(ms);
                ms.Position = 0;
                streamToDecode = ms;
            }
            else
            {
                inputStream.Position = 0;
                streamToDecode = inputStream;
            }

            using (var skBitmap = SKBitmap.Decode(streamToDecode))
            {
                if (skBitmap == null)
                {
                    throw new Exception("Failed to decode the image.");
                }

                int originalWidth = skBitmap.Width;
                int originalHeight = skBitmap.Height;
                bool needsResize = originalWidth > maxWidth || originalHeight > maxHeight;
                SKBitmap bitmapToEncode = skBitmap;
                SKBitmap? resizedBitmap = null;

                if (needsResize)
                {
                    double widthRatio = (double)maxWidth / originalWidth;
                    double heightRatio = (double)maxHeight / originalHeight;
                    double scaleFactor = Math.Min(widthRatio, heightRatio);
                    int newWidth = (int)(originalWidth * scaleFactor);
                    int newHeight = (int)(originalHeight * scaleFactor);

                    resizedBitmap = skBitmap.Resize(new SKImageInfo(newWidth, newHeight), new SKSamplingOptions(SKFilterMode.Nearest));
                    if (resizedBitmap != null)
                    {
                        bitmapToEncode = resizedBitmap;
                    }
                }

                using (var image = SKImage.FromBitmap(bitmapToEncode))
                using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 80))
                using (var fileStream = System.IO.File.OpenWrite(filePath))
                {
                    await data.AsStream().CopyToAsync(fileStream);
                }

                resizedBitmap?.Dispose();
            }

            if (!inputStream.CanSeek)
            {
                streamToDecode.Dispose();
            }
        }

        #endregion
    }
}