using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;

namespace RegexFileSearcher
{
    public static class CompressedFileWalker
    {
        public static IEnumerable<FilePath> GetCompressedFiles(FilePath filePath)
        {
            List<FilePath> results = new List<FilePath>();
            if (!IsZipFile(filePath.Path))
            {
                return results;
            }

            try
            {
                using FileStream zipToOpen = new FileStream(filePath.Path, FileMode.Open);
                using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read);
                results.AddRange(GetCompressedFilesInner(filePath, archive.Entries));
            }
            catch
            {
                // Any zip file related exception
            }

            return results;
        }

        private static IEnumerable<FilePath> GetCompressedFilesInner(FilePath filePath,
            IEnumerable<ZipArchiveEntry> archiveEntries)
        {
            foreach (ZipArchiveEntry archiveEntry in archiveEntries)
            {
                string archiveEntryName = archiveEntry.FullName;
                filePath.CompressedFile = new FilePath(archiveEntryName);
                if (IsZipFile(archiveEntryName))
                {
                    Stream entryStream = null;
                    try
                    {
                        entryStream = archiveEntry.Open();
                    }
                    catch
                    {
                        // ZipArchiveEntry.Open() related issues
                    }

                    if (entryStream != null)
                    {
                        var innerFilePath = new FilePath(archiveEntryName, filePath);
                        foreach (FilePath compressedFile in GetCompressedFiles(filePath.CompressedFile, entryStream))
                        {
                            yield return compressedFile;
                        }
                    }
                }
                else
                {
                    yield return filePath;
                }
            }
        }

        private static IEnumerable<FilePath> GetCompressedFiles(FilePath compressedFilePath,
            Stream compressedZipFile)
        {
            List<FilePath> results = new List<FilePath>();
            try
            {
                using ZipArchive archive = new ZipArchive(compressedZipFile, ZipArchiveMode.Read);
                results.AddRange(GetCompressedFilesInner(compressedFilePath, archive.Entries));
            }
            catch
            {
                // new ZipArchive() related issues
            }

            return results;
        }

        private static bool IsZipFile(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension == ".zip" || extension == ".gz";
        }
    }
}
