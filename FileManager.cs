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
		private FileSystemWatcher watcher;

		public HashSet<string> ImageFiles { get; private set; } = new HashSet<string>();


		public FileManager()
		{
			BaseFolder = GetBaseFolder();
			IconsFolder = GetIconsFolder();

			CreateFolder(BaseFolder);
			CreateFolder(IconsFolder);

			LoadExistingImages(); // Load images on startup
			StartWatchingIconsFolder(); // Start monitoring folder
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

		private void LoadExistingImages()
		{
			try
			{
				// Get all PNG files
				string[] files = Directory.GetFiles(IconsFolder, "*.png");
				Console.WriteLine("__ [ Files in Dir ] __");
				ImageFiles.Clear(); // Clear existing files to avoid duplicates
									// Add only unique files (HashSet ensures no duplicates)
				foreach (string file in files)
				{
					ImageFiles.Add(file); // HashSet's Add() prevents duplicates
				}

				foreach (string file in ImageFiles)
				{
					Console.WriteLine($"Loaded image: {file}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading images: {ex.Message}");
			}
		}

		public void StartWatchingIconsFolder()
		{
			watcher = new FileSystemWatcher(IconsFolder, "*.png");
			watcher.Created += (s, e) =>
			{
				if (ImageFiles.Add(e.FullPath)) // HashSet ensures uniqueness
				{
					Console.WriteLine($"New image added: {e.FullPath}");
				}
			};
			watcher.EnableRaisingEvents = true;
		}

		public void OpenBaseFolder()
		{
			OpenFolder(BaseFolder);
		}
	}
}