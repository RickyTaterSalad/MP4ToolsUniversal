using System;
using System.IO;
using System.Linq;

namespace MP4ToolsLib;

/// <summary>
/// Volume / mount helpers: same-disk checks, free space, and removable/SD detection.
/// </summary>
public static class VolumePathHelper
{
	public static bool AreSameVolume(string pathA, string pathB)
	{
		try
		{
			var rootA = GetVolumeRoot(pathA);
			var rootB = GetVolumeRoot(pathB);
			if (string.IsNullOrEmpty(rootA) || string.IsNullOrEmpty(rootB))
			{
				return string.Equals(
					ResolveExistingDirectory(pathA),
					ResolveExistingDirectory(pathB),
					StringComparison.OrdinalIgnoreCase);
			}

			return string.Equals(rootA, rootB, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public static long TryGetAvailableFreeSpace(string path)
	{
		try
		{
			var probe = ResolveExistingDirectory(path);
			if (string.IsNullOrWhiteSpace(probe))
				return -1;

			if (OperatingSystem.IsWindows())
			{
				var root = Path.GetPathRoot(probe);
				if (string.IsNullOrWhiteSpace(root))
					return -1;
				return new DriveInfo(root).AvailableFreeSpace;
			}

			// On Unix, DriveInfo accepts any directory on the target filesystem.
			return new DriveInfo(probe).AvailableFreeSpace;
		}
		catch
		{
			return -1;
		}
	}

	/// <summary>
	/// True when <paramref name="path"/> appears to live on an SD card or other removable media.
	/// Source videos on these volumes must never be deleted.
	/// </summary>
	public static bool IsLikelyRemovableOrSdMedia(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return false;

		try
		{
			var full = Path.GetFullPath(path);

			if (OperatingSystem.IsWindows())
			{
				var root = Path.GetPathRoot(full);
				if (string.IsNullOrWhiteSpace(root))
					return false;
				return new DriveInfo(root).DriveType == DriveType.Removable;
			}

			// Desktop automount roots for camera SD / USB sticks.
			if (IsUnderPrefix(full, "/media/") || IsUnderPrefix(full, "/run/media/"))
				return true;

			var device = FindUnixMountDevice(full);
			if (string.IsNullOrEmpty(device))
				return false;

			// SD / eMMC block devices (camera cards typically show up as mmcblk*).
			if (device.Contains("mmcblk", StringComparison.OrdinalIgnoreCase))
				return true;

			// USB sticks and other removable disks (including under /mnt).
			if (IsUnixSysRemovable(device))
				return true;
		}
		catch
		{
			// ignored — fail open (do not claim removable)
		}

		return false;
	}

	public static string ResolveExistingDirectory(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return null;

		try
		{
			var full = Path.GetFullPath(path);
			if (Directory.Exists(full))
				return full;
			if (File.Exists(full))
				return Path.GetDirectoryName(full);

			var current = full;
			while (!string.IsNullOrEmpty(current))
			{
				if (Directory.Exists(current))
					return current;
				var parent = Path.GetDirectoryName(current);
				if (string.IsNullOrEmpty(parent) || parent == current)
					break;
				current = parent;
			}
		}
		catch
		{
			// ignored
		}

		return null;
	}

	public static string GetVolumeRoot(string path)
	{
		var probe = ResolveExistingDirectory(path);
		if (string.IsNullOrWhiteSpace(probe))
			return null;

		try
		{
			var full = Path.GetFullPath(probe);
			var match = DriveInfo.GetDrives()
				.Where(d =>
				{
					try
					{
						if (!d.IsReady || string.IsNullOrEmpty(d.Name))
							return false;
						var name = d.Name.TrimEnd(Path.DirectorySeparatorChar);
						if (string.IsNullOrEmpty(name))
							name = d.Name; // "/" on Unix
						if (string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), name, StringComparison.OrdinalIgnoreCase))
							return true;
						var prefix = name.EndsWith(Path.DirectorySeparatorChar)
							? name
							: name + Path.DirectorySeparatorChar;
						return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
					}
					catch
					{
						return false;
					}
				})
				.OrderByDescending(d => d.Name.Length)
				.FirstOrDefault();

			return match?.Name;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsUnderPrefix(string fullPath, string prefix) =>
		fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

	private static string FindUnixMountDevice(string fullPath)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
			return null;

		try
		{
			var mountsPath = OperatingSystem.IsLinux() ? "/proc/mounts" : "/etc/mtab";
			if (!File.Exists(mountsPath))
				return null;

			string bestMount = null;
			string bestDevice = null;
			foreach (var line in File.ReadLines(mountsPath))
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
					continue;
				var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2)
					continue;

				var device = UnescapeMountField(parts[0]);
				var mountPoint = UnescapeMountField(parts[1]);
				if (string.IsNullOrEmpty(mountPoint))
					continue;

				var mountFull = mountPoint.TrimEnd(Path.DirectorySeparatorChar);
				if (string.IsNullOrEmpty(mountFull))
					mountFull = "/";

				bool matches =
					string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar), mountFull, StringComparison.Ordinal)
					|| fullPath.StartsWith(
						mountFull == "/" ? "/" : mountFull + Path.DirectorySeparatorChar,
						StringComparison.Ordinal);

				if (!matches)
					continue;

				if (bestMount == null || mountFull.Length > bestMount.Length)
				{
					bestMount = mountFull;
					bestDevice = device;
				}
			}

			return bestDevice;
		}
		catch
		{
			return null;
		}
	}

	private static string UnescapeMountField(string value) =>
		(value ?? string.Empty)
			.Replace("\\040", " ", StringComparison.Ordinal)
			.Replace("\\011", "\t", StringComparison.Ordinal)
			.Replace("\\012", "\n", StringComparison.Ordinal)
			.Replace("\\134", "\\", StringComparison.Ordinal);

	private static bool IsUnixSysRemovable(string devicePath)
	{
		if (string.IsNullOrWhiteSpace(devicePath) || !devicePath.StartsWith("/dev/", StringComparison.Ordinal))
			return false;

		try
		{
			var name = ResolveUnixBlockName(devicePath["/dev/".Length..]);
			if (string.IsNullOrEmpty(name))
				return false;

			var removablePath = $"/sys/block/{name}/removable";
			if (!File.Exists(removablePath))
				return false;
			var text = File.ReadAllText(removablePath).Trim();
			return text == "1";
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Map partition names to parent block device (sdb1→sdb, mmcblk0p1→mmcblk0).</summary>
	private static string ResolveUnixBlockName(string partitionOrDisk)
	{
		var name = partitionOrDisk ?? string.Empty;
		if (name.StartsWith("mmcblk", StringComparison.OrdinalIgnoreCase)
			|| name.StartsWith("nvme", StringComparison.OrdinalIgnoreCase))
		{
			var p = name.LastIndexOf('p');
			if (p > 0 && p < name.Length - 1 && name[(p + 1)..].All(char.IsDigit))
				return name[..p];
			return name;
		}

		while (name.Length > 0 && char.IsDigit(name[^1]))
			name = name[..^1];
		return name;
	}
}
