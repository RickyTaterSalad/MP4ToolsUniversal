using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace MP4ToolsLib
{
    public enum CombineFileSortMode
    {
        /// <summary>File creation time, oldest first.</summary>
        Creation = 0,
        /// <summary>File name, ordinal ignore-case.</summary>
        FileName = 1,
    }

    public class CombineFile
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        private static readonly string[] VideoExtensions = { ".mp4", ".mov" };

        public static bool IsSupportedVideoFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            var ext = System.IO.Path.GetExtension(path);
            return !string.IsNullOrEmpty(ext)
                && VideoExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Builds a one-item list from a single .mp4/.mov file path.</summary>
        public static IReadOnlyList<CombineFile> FromVideoFile(string path)
        {
            if (!IsSupportedVideoFile(path))
                return Array.Empty<CombineFile>();

            var fileInfo = new FileInfo(path);
            return new[]
            {
                new CombineFile
                {
                    Name = fileInfo.Name,
                    Path = fileInfo.FullName
                }
            };
        }

        public static IReadOnlyList<CombineFile> FromFolder(
            string folder,
            CombineFileSortMode sortMode = CombineFileSortMode.Creation,
            bool reverseList = false)
        {
            var fileList = new List<CombineFile>();
            
            if (Directory.Exists(folder))
            {
                try
                {
                    var mp4Files = Directory.GetFiles(folder)
                        .Where(x => VideoExtensions.Contains(System.IO.Path.GetExtension(x), StringComparer.OrdinalIgnoreCase));

                    var ordered = sortMode == CombineFileSortMode.FileName
                        ? mp4Files.OrderBy(f => System.IO.Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                        : mp4Files.OrderBy(f => new FileInfo(f).CreationTime);

                    var list = ordered.ToList();
                    
                    if (reverseList)
                    {
                        list.Reverse();
                    }

                    foreach (var file in list)
                    {
                        var fileInfo = new FileInfo(file);
                        fileList.Add(new CombineFile()
                        {
                            Name = fileInfo.Name,
                            Path = fileInfo.FullName
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception appropriately
                    // For now, we'll just return empty list
                    Debug.WriteLine($"Error processing folder {folder}: {ex.Message}");
                }
            }
            
            return fileList;
        }

        /// <summary>Returns a new list sorted by the given mode (does not mutate <paramref name="files"/>).</summary>
        public static List<CombineFile> Sort(IEnumerable<CombineFile> files, CombineFileSortMode sortMode)
        {
            var list = files?.Where(f => f != null).ToList() ?? new List<CombineFile>();
            if (list.Count <= 1)
                return list;

            return sortMode == CombineFileSortMode.FileName
                ? list.OrderBy(f => f.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList()
                : list.OrderBy(f =>
                {
                    try { return File.GetCreationTime(f.Path); }
                    catch { return DateTime.MaxValue; }
                }).ToList();
        }
    }
}
