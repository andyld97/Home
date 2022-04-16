using System.Collections.Generic;
using System.Threading.Tasks;

namespace Home.Data.Helper
{   
    public static class ZipHelper
    {
        public static async Task CreateZipFileFromDirectoryAsync(string sourcePath, string destionationFile)
        {
            if (!System.IO.Directory.Exists(sourcePath))
                throw new System.IO.DirectoryNotFoundException(sourcePath);

            Queue<string> folderQueue = new Queue<string>();
            folderQueue.Enqueue(sourcePath);

            using (System.IO.FileStream fs = new System.IO.FileStream(destionationFile, System.IO.FileMode.Create))
            using (System.IO.Compression.ZipArchive zipArchive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create, false))
            {
                while (folderQueue.Count != 0)
                {
                    System.IO.DirectoryInfo folderInfo = new System.IO.DirectoryInfo(folderQueue.Dequeue());

                    foreach (var subDir in folderInfo.EnumerateDirectories("*.*", System.IO.SearchOption.TopDirectoryOnly))
                        folderQueue.Enqueue(subDir.FullName);

                    foreach (var file in folderInfo.GetFiles("*.*", System.IO.SearchOption.TopDirectoryOnly))
                    {
                        string entryPath = System.IO.PathNetCore.GetRelativePath(sourcePath, file.FullName);
                        var e = zipArchive.CreateEntry(entryPath);

                        using (var entry = e.Open())
                        using (System.IO.FileStream fi = new System.IO.FileStream(file.FullName, System.IO.FileMode.Open))
                            await fi.CopyToAsync(entry);
                    }
                }
            }
        }

        public static void CreateZipFileFromDirectory(string sourcePath, string destionationFile)
        {
            if (!System.IO.Directory.Exists(sourcePath))
                throw new System.IO.DirectoryNotFoundException(sourcePath);

            Queue<string> folderQueue = new Queue<string>();
            folderQueue.Enqueue(sourcePath);

            using (System.IO.FileStream fs = new System.IO.FileStream(destionationFile, System.IO.FileMode.Create))
            using (System.IO.Compression.ZipArchive zipArchive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create, false))
            {
                while (folderQueue.Count != 0)
                {
                    System.IO.DirectoryInfo folderInfo = new System.IO.DirectoryInfo(folderQueue.Dequeue());

                    foreach (var subDir in folderInfo.EnumerateDirectories("*.*", System.IO.SearchOption.TopDirectoryOnly))
                        folderQueue.Enqueue(subDir.FullName);

                    foreach (var file in folderInfo.GetFiles("*.*", System.IO.SearchOption.TopDirectoryOnly))
                    {
                        string entryPath = System.IO.PathNetCore.GetRelativePath(sourcePath, file.FullName);
                        var e = zipArchive.CreateEntry(entryPath);

                        using (var entry = e.Open())
                        using (System.IO.FileStream fi = new System.IO.FileStream(file.FullName, System.IO.FileMode.Open))
                            fi.CopyTo(entry);
                    }
                }
            }
        }

        public static async Task ExtractZipFileAsync(string sourceFilePath, string targetDirectoryPath)
        {
            if (!System.IO.Directory.Exists(targetDirectoryPath))
                System.IO.Directory.CreateDirectory(targetDirectoryPath);

            using (System.IO.FileStream fs = new System.IO.FileStream(sourceFilePath, System.IO.FileMode.Open))
            using (System.IO.Compression.ZipArchive zipArchive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, false))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    // Get the target file path and the parent directory
                    string targetPath = System.IO.Path.Combine(targetDirectoryPath, entry.FullName);
                    string parentDirectory = System.IO.Path.GetDirectoryName(targetPath);

                    // Create parent directory (if it doesn't exists)
                    if (!System.IO.Directory.Exists(parentDirectory))
                        System.IO.Directory.CreateDirectory(parentDirectory);

                    // Ignore empty folder entries
                    if (string.IsNullOrEmpty(entry.Name) && entry.Length == 0)
                        continue;

                    // Write the file to disk
                    using (System.IO.FileStream stream = new System.IO.FileStream(targetPath, System.IO.FileMode.Create))
                    using (var e = entry.Open())
                        await e.CopyToAsync(stream);
                }
            }
        }

        public static async Task ExtractZipFileAsync(byte[] data, string targetDirectoryPath)
        {
            if (!System.IO.Directory.Exists(targetDirectoryPath))
                System.IO.Directory.CreateDirectory(targetDirectoryPath);

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
            using (System.IO.Compression.ZipArchive zipArchive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read, false))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    // Get the target file path and the parent directory
                    string targetPath = System.IO.Path.Combine(targetDirectoryPath, entry.FullName);
                    string parentDirectory = System.IO.Path.GetDirectoryName(targetPath);

                    // Create parent directory (if it doesn't exists)
                    if (!System.IO.Directory.Exists(parentDirectory))
                        System.IO.Directory.CreateDirectory(parentDirectory);

                    // Ignore empty folder entries
                    if (string.IsNullOrEmpty(entry.Name) && entry.Length == 0)
                        continue;

                    // Write the file to disk
                    using (System.IO.FileStream stream = new System.IO.FileStream(targetPath, System.IO.FileMode.Create))
                    using (var e = entry.Open())
                        await e.CopyToAsync(stream);
                }
            }
        }

        public static void ExtractZipFile(string sourceFilePath, string targetDirectoryPath)
        {
            if (!System.IO.Directory.Exists(targetDirectoryPath))
                System.IO.Directory.CreateDirectory(targetDirectoryPath);

            using (System.IO.FileStream fs = new System.IO.FileStream(sourceFilePath, System.IO.FileMode.Open))
            using (System.IO.Compression.ZipArchive zipArchive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, false))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    // Get the target file path and the parent directory
                    string targetPath = System.IO.Path.Combine(targetDirectoryPath, entry.FullName);
                    string parentDirectory = System.IO.Path.GetDirectoryName(targetPath);

                    // Create parent directory (if it doesn't exists)
                    if (!System.IO.Directory.Exists(parentDirectory))
                        System.IO.Directory.CreateDirectory(parentDirectory);

                    // Ignore empty folder entries
                    if (string.IsNullOrEmpty(entry.Name) && entry.Length == 0)
                        continue;

                    // Write the file to disk
                    using (System.IO.FileStream stream = new System.IO.FileStream(targetPath, System.IO.FileMode.Create))
                    using (var e = entry.Open())
                        e.CopyTo(stream);
                }
            }
        }

        public static void ExtractZipFile(byte[] data, string targetDirectoryPath)
        {
            if (!System.IO.Directory.Exists(targetDirectoryPath))
                System.IO.Directory.CreateDirectory(targetDirectoryPath);

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
            using (System.IO.Compression.ZipArchive zipArchive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read, false))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    // Get the target file path and the parent directory
                    string targetPath = System.IO.Path.Combine(targetDirectoryPath, entry.FullName);
                    string parentDirectory = System.IO.Path.GetDirectoryName(targetPath);

                    // Create parent directory (if it doesn't exists)
                    if (!System.IO.Directory.Exists(parentDirectory))
                        System.IO.Directory.CreateDirectory(parentDirectory);

                    // Ignore empty folder entries
                    if (string.IsNullOrEmpty(entry.Name) && entry.Length == 0)
                        continue;

                    // Write the file to disk
                    using (System.IO.FileStream stream = new System.IO.FileStream(targetPath, System.IO.FileMode.Create))
                    using (var e = entry.Open())
                        e.CopyTo(stream);
                }
            }
        }
    }
}