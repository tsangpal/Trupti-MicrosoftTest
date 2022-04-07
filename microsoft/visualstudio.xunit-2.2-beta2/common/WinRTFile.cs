using Windows.ApplicationModel;
using Windows.Storage;

namespace System.IO
{
    static class File
    {
        public static bool Exists(string path)
        {
            return GetStorageFile(path) != null;
        }

        public static Stream OpenRead(string path)
        {
            var storageFile = GetStorageFile(path);
            if (storageFile == null)
                throw new FileNotFoundException("Could not open file for read", path);

            return storageFile.OpenStreamForReadAsync().GetAwaiter().GetResult();
        }

        // Helpers

        static StorageFile GetStorageFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var folder = Package.Current.InstalledLocation;

            if (Path.GetDirectoryName(path) != string.Empty && !path.Contains(folder.Path))
                return null;

            var fileName = Path.GetFileName(path);

            try
            {
                var fileAsync = folder.GetFileAsync(fileName);
                return fileAsync.AsTask().GetAwaiter().GetResult();
            }
            catch
            {
                // any errors, we juts can't get it
            }

            return null;
        }
    }
}
