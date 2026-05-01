using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace MP4ToolsLib
{
    public class CombineFile
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public static IReadOnlyList<CombineFile> FromFolder(string folder, bool reverseList = false)
        {
            var fileList = new List<CombineFile>();

            if (Directory.Exists(folder))
            {
                try
                {
                    string[] extensions = new[] { ".mp4", ".mov" };
                    var mp4Files = Directory.GetFiles(folder)
                        .Where(x => extensions.Contains(System.IO.Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
                        .OrderBy(f => new FileInfo(f).CreationTime)
                        .ToList();

                    if (reverseList)
                    {
                        mp4Files.Reverse();
                    }

                    foreach (var file in mp4Files)
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
    }
}