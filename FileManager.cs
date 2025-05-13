using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using XeoClip2;

namespace XeoClip2
{
	public class FileManager
	{
		public string BaseFolder { get; private set; }
		public string IconsFolder { get; private set; }


		public FileManager()
		{
			BaseFolder = GetBaseFolder();
			IconsFolder = GetIconsFolder();

			CreateFolder(BaseFolder);
			CreateFolder(IconsFolder);
		}

		private string GetBaseFolder()
		{
			return Path.Combine(Path.GetTempPath(), "XeoClip2");
		}

		private string GetIconsFolder()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XeoClip2", "Images");
		}

		private void CreateFolder(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		public void OpenFolder(string folderPath)
		{
			if (Directory.Exists(folderPath))
			{
				Process.Start("explorer.exe", folderPath);
			}
			else
			{
				throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");
			}
		}

		public void OpenBaseFolder()
		{
			OpenFolder(BaseFolder);
		}
	}
}