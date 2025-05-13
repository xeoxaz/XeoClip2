using System;
using System.IO;

namespace XeoClip2
{
	public class FileManager
	{
		public string BaseFolder { get; private set; }

		public FileManager()
		{
			string tempPath = Path.GetTempPath();
			BaseFolder = Path.Combine(tempPath, "XeoClip2");
			Directory.CreateDirectory(BaseFolder);
		}

		public void OpenBaseFolder()
		{
			if (Directory.Exists(BaseFolder))
			{
				System.Diagnostics.Process.Start("explorer.exe", BaseFolder);
			}
			else
			{
				throw new Exception("The base folder does not exist.");
			}
		}
	}
}