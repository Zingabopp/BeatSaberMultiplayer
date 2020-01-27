using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.Misc
{
    public static class ZipUtilities
    {
        public static readonly int MaxFileSystemPathLength = 259;

        public static ZipExtractResult ExtractZip(string zipPath, string extractDirectory, bool overwriteTarget = true)
        {
            if (string.IsNullOrEmpty(zipPath))
                return new ZipExtractResult()
                {
                    CreatedOutputDirectory = false,
                    Exception = new ArgumentNullException(nameof(zipPath), "zipPath cannot be null or empty for ExtractZip"),
                    ExtractedFiles = Array.Empty<string>(),
                    SourceZip = zipPath,
                    ResultStatus = ZipExtractResultStatus.SourceFailed,
                    OutputDirectory = extractDirectory
                };
            FileInfo zipFile = new FileInfo(zipPath);
            if (!zipFile.Exists)
                return new ZipExtractResult()
                {
                    CreatedOutputDirectory = false,
                    Exception = new ArgumentException($"File at zipPath {zipFile.FullName} does not exist.", nameof(zipPath)),
                    ExtractedFiles = Array.Empty<string>(),
                    SourceZip = zipPath,
                    ResultStatus = ZipExtractResultStatus.SourceFailed,
                    OutputDirectory = extractDirectory
                };


            using (var zipStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            {
                ZipExtractResult result = ExtractZip(zipStream, extractDirectory, overwriteTarget);
                result.SourceZip = zipPath;
                return result;
            }
        }

        /// <summary>
        /// Extracts a zip file to the specified directory. If an exception is thrown during extraction, it is stored in ZipExtractResult.
        /// </summary>
        /// <param name="zipPath">Path to zip file</param>
        /// <param name="extractDirectory">Directory to extract to</param>
        /// <param name="overwriteTarget">If true, overwrites existing files with the zip's contents</param>
        /// <returns></returns>
        public static ZipExtractResult ExtractZip(Stream zipStream, string extractDirectory, bool overwriteTarget = true)
        {
            if (zipStream == null)
                return new ZipExtractResult()
                {
                    CreatedOutputDirectory = false,
                    Exception = new ArgumentNullException(nameof(zipStream), "zipStream cannot be null for ExtractZip"),
                    ExtractedFiles = Array.Empty<string>(),
                    SourceZip = null,
                    ResultStatus = ZipExtractResultStatus.SourceFailed,
                    OutputDirectory = extractDirectory
                };
            if (string.IsNullOrEmpty(extractDirectory))
                return new ZipExtractResult()
                {
                    CreatedOutputDirectory = false,
                    Exception = new ArgumentNullException(nameof(extractDirectory), "extractDirectory cannot be null or empty for ExtractZip"),
                    ExtractedFiles = Array.Empty<string>(),
                    SourceZip = null,
                    ResultStatus = ZipExtractResultStatus.SourceFailed,
                    OutputDirectory = extractDirectory
                };



            ZipExtractResult result = new ZipExtractResult
            {
                SourceZip = null,
                ResultStatus = ZipExtractResultStatus.Unknown
            };

            string createdDirectory = null;
            var createdFiles = new List<string>();
            try
            {
                //Logger.log?.Info($"ExtractDirectory is {extractDirectory}");
                using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    //Logger.log?.Info("Zip opened");
                    //extractDirectory = GetValidPath(extractDirectory, zipArchive.Entries.Select(e => e.Name).ToArray(), shortDirName, overwriteTarget);
                    var longestEntryName = zipArchive.Entries.Select(e => e.Name).Max(n => n.Length);
                    try
                    {
                        extractDirectory = Path.GetFullPath(extractDirectory); // Could theoretically throw an exception: Argument/ArgumentNull/Security/NotSupported/PathTooLong
                        extractDirectory = GetValidPath(extractDirectory, longestEntryName, 3);
                        if (!overwriteTarget && Directory.Exists(extractDirectory))
                        {
                            int pathNum = 1;
                            string finalPath;
                            do
                            {
                                var append = $" ({pathNum})";
                                finalPath = GetValidPath(extractDirectory, longestEntryName, append.Length) + append; // padding ensures we aren't continuously cutting off the append value
                                pathNum++;
                            } while (Directory.Exists(finalPath));
                            extractDirectory = finalPath;
                        }
                    }
                    catch (PathTooLongException ex)
                    {
                        result.Exception = ex;
                        result.ResultStatus = ZipExtractResultStatus.DestinationFailed;
                        return result;
                    }
                    result.OutputDirectory = extractDirectory;
                    bool extractDirectoryExists = Directory.Exists(extractDirectory);
                    var toBeCreated = extractDirectoryExists ? null : extractDirectory; // For cleanup
                    try { Directory.CreateDirectory(extractDirectory); }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                        result.ResultStatus = ZipExtractResultStatus.DestinationFailed;
                        return result;
                    }

                    result.CreatedOutputDirectory = !extractDirectoryExists;
                    createdDirectory = string.IsNullOrEmpty(toBeCreated) ? null : extractDirectory;
                    // Ordering so largest files extracted first. If the extraction is interrupted, theoretically the song's hash won't match Beat Saver's.
                    foreach (var entry in zipArchive.Entries.OrderByDescending(e => e.Length))
                    {
                        if (!entry.FullName.Equals(entry.Name)) // If false, the entry is a directory or file nested in one
                            continue;
                        var entryPath = Path.Combine(extractDirectory, entry.Name);
                        var fileExists = File.Exists(entryPath);
                        if (overwriteTarget || !fileExists)
                        {
                            try
                            {
                                entry.ExtractToFile(entryPath, overwriteTarget);
                                createdFiles.Add(entryPath);
                            }
                            catch (InvalidDataException ex) // Entry is missing, corrupt, or compression method isn't supported
                            {
                                Plugin.log?.Error($"Error extracting {extractDirectory}, archive appears to be damaged.");
                                Plugin.log?.Error(ex);
                                result.Exception = ex;
                                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                                result.ExtractedFiles = createdFiles.ToArray();
                            }
                            catch (Exception ex)
                            {
                                Plugin.log?.Error($"Error extracting {extractDirectory}");
                                Plugin.log?.Error(ex);
                                result.Exception = ex;
                                result.ResultStatus = ZipExtractResultStatus.DestinationFailed;
                                result.ExtractedFiles = createdFiles.ToArray();

                            }
                            if (result.Exception != null)
                            {
                                foreach (var file in createdFiles)
                                {
                                    TryDelete(file);
                                }
                                return result;
                            }
                        }
                    }
                    result.ExtractedFiles = createdFiles.ToArray();
                }
                result.ResultStatus = ZipExtractResultStatus.Success;
                return result;
#pragma warning disable CA1031 // Do not catch general exception types
            }
            catch (InvalidDataException ex) // Stream is not in the zip archive format.
            {
                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                result.Exception = ex;
                return result;
            }
            catch (Exception ex) // If exception is thrown here, it probably happened when the ZipArchive was created.
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Plugin.log?.Error($"Error opening Stream as a ZipArchive (probably).");
                Plugin.log?.Error(ex);
                try
                {
                    if (!string.IsNullOrEmpty(createdDirectory))
                    {
                        Directory.Delete(createdDirectory, true);
                    }
                    else // TODO: What is this doing here...
                    {
                        foreach (var file in createdFiles)
                        {
                            File.Delete(file);
                        }
                    }
                }
                catch (Exception cleanUpException)
                {
                    // Failed at cleanup
                    Plugin.log?.Debug($"Failed to clean up zip file: {cleanUpException.Message}");
                }

                result.Exception = ex;
                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extractDirectory"></param>
        /// <param name="longestEntryName"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        /// <exception cref="PathTooLongException">Thrown if shortening the path enough is impossible.</exception>
        public static string GetValidPath(string extractDirectory, int longestEntryName, int padding = 0)
        {
            var extLength = extractDirectory.Length;
            var dir = new DirectoryInfo(extractDirectory);
            int minLength = dir.Parent.FullName.Length + 2;
            var dirName = dir.Name;
            var diff = MaxFileSystemPathLength - extLength - longestEntryName - padding;
            if (diff < 0)
            {

                if (dirName.Length + diff > 0)
                {
                    //Logger.log?.Warn($"{extractDirectory} is too long, attempting to shorten.");
                    extractDirectory = extractDirectory.Substring(0, minLength + dirName.Length + diff);
                }
                else
                {
                    //Logger.log?.Error($"{extractDirectory} is too long, couldn't shorten enough.");
                    throw new PathTooLongException(extractDirectory);
                }
            }
            return extractDirectory;
        }

        public static bool TryDelete(string filePath)
        {
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.log?.Error($"Unable to delete {filePath}: {ex.Message}");
                Plugin.log?.Debug(ex);
                return false;
            }
        }


        public class ZipExtractResult
        {
            public string SourceZip { get; internal set; }
            public string OutputDirectory { get; internal set; }
            public bool CreatedOutputDirectory { get; internal set; }
            public string[] ExtractedFiles { get; internal set; }
            public ZipExtractResultStatus ResultStatus { get; internal set; }
            public Exception Exception { get; internal set; }
        }

        public enum ZipExtractResultStatus
        {
            /// <summary>
            /// Extraction hasn't been attempted.
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Extraction was successful.
            /// </summary>
            Success = 1,
            /// <summary>
            /// Problem with the zip source.
            /// </summary>
            SourceFailed = 2,
            /// <summary>
            /// Problem with the destination target.
            /// </summary>
            DestinationFailed = 3
        }
    }
}
