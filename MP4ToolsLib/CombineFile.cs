using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MP4ToolsLib
{
	public class CombineFile
	{
		public string Name { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;

		public override string ToString()
		{
			return Name ?? string.Empty;
		}

		public static IReadOnlyList<CombineFile> FromFolder(string folder, bool reverseList = false)
		{
			var fileList = new List<CombineFile>();
			if (Directory.Exists(folder))
			{
				try
				{
					string[] extensions = new[] { ".mp4", ".mov", ".MP4", ".MOV" };
					var mp4Files = new DirectoryInfo(folder).GetFiles().Where(x => extensions.Contains(x.Extension)).OrderBy(f => f.CreationTime).ToList();
					if (reverseList)
					{
						mp4Files.Reverse();
					}

					var arr = mp4Files.Select(x => new CombineFile() { Name = x.Name, Path = x.FullName }).ToArray();
					fileList.AddRange(arr);
				}
				catch
				{
					//
				}
			}
			return fileList;
		}
	}
}
