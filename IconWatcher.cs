using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace XeoClip2
{
	internal class IconWatcher
	{

		// Update the nullable reference type declaration to remove the nullable annotation (?)  
		private Thread watcherThread;
		private volatile bool isWatching;

		// Replace the target-typed object creation with explicit type initialization to ensure compatibility with C# 7.3.
		private readonly List<TimeSpan> detectionTimestamps = new List<TimeSpan>(); // Buffer to store timestamps

		public IconWatcher() {
			//
			// xeoxaz was here :)
			//
		}

		public void StartWatching(DateTime recordingStartTime)
		{
			if (isWatching)
			{
				Console.WriteLine("IconWatcher is already running.");
				return;
			}

			var manager = new FileManager();
			var icons = LoadIcons(manager.IconsFolder);

			if (icons.Length == 0)
			{
				Console.WriteLine("No icons found in the icons folder. IconWatcher will not run.");
				return;
			}

			isWatching = true;
			Console.WriteLine("Starting IconWatcher...");

			watcherThread = new Thread(() =>
			{
				try
				{
					while (isWatching)
					{
						using (var screenshot = CaptureScreen()) // Proper disposal
						{
							if (!TryDetectIcons(screenshot, icons))
							{
								Thread.Sleep(100); // Tune scan aggression
								continue;
							}

							DateTime detectionTime = DateTime.Now;
							TimeSpan relativeTimestamp = detectionTime - recordingStartTime;
							string startTime = relativeTimestamp.ToString(@"hh\:mm\:ss\.fff");

							Console.WriteLine($"Icon detected at {detectionTime}, Relative Timestamp: {startTime}. Checking timestamp gap...");

							bool validDetection = false;

							// Thread-safe timestamp storage and gap enforcement
							lock (detectionTimestamps)
							{
								if (detectionTimestamps.Count > 0)
								{
									TimeSpan lastTimestamp = detectionTimestamps.Last();
									TimeSpan gap = relativeTimestamp - lastTimestamp;

									if (gap >= TimeSpan.FromSeconds(15)) // Enforce minimum detection gap
									{
										validDetection = true;
									}
									else
									{
										Console.WriteLine($"Detection too close to previous ({gap.TotalSeconds:F3}s). Ignoring.");
									}
								}
								else
								{
									validDetection = true; // First detection allowed
								}

								if (validDetection)
								{
									detectionTimestamps.Add(relativeTimestamp);
									Console.WriteLine("Timestamp recorded. Continuing icon watching...");
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error in IconWatcher thread: {ex.Message}\n{ex.StackTrace}");
				}
				finally
				{
					isWatching = false;
					Console.WriteLine("IconWatcher stopped.");
				}
			})
			{ IsBackground = true };

			watcherThread.Start();
		}




		public async Task StopWatchingAsync()
		{
			if (!isWatching)
			{
				Console.WriteLine("IconWatcher is not running.");
				return;
			}

			Console.WriteLine("Stopping IconWatcher...");
			isWatching = false;

			if (watcherThread != null)
			{
				await Task.Run(() => watcherThread.Join());
			}

			Console.WriteLine("IconWatcher has stopped.");
		}

		public List<TimeSpan> GetBufferedTimestamps()
		{
			return new List<TimeSpan>(detectionTimestamps);
		}

		public void ClearBufferedTimestamps()
		{
			detectionTimestamps.Clear();
			Console.WriteLine("Buffered timestamps have been cleared.");
		}

		private Mat[] LoadIcons(string path)
		{
			var files = Directory.GetFiles(path, "*.png");
			Console.WriteLine($"Found {files.Length} icon(s) in {path}.");
			return Array.ConvertAll(files, file => Cv2.ImRead(file, ImreadModes.Grayscale));
		}

		private Bitmap CaptureScreen()
		{
			var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
			var screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

			var graphics = Graphics.FromImage(screenshot);
			graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

			graphics.Dispose(); // Explicitly dispose the Graphics object

			return screenshot;
		}


		private bool TryDetectIcons(Bitmap screenshot, Mat[] icons)
		{
			var frame = OpenCvSharp.Extensions.BitmapConverter.ToMat(screenshot); // Removed using

			foreach (var icon in icons)
			{
				if (DetectIcon(frame, icon, out double matchValue))
				{
					Console.WriteLine($"Match Value: {matchValue:F2} (Threshold: 0.8)");
					frame.Dispose(); // Explicitly dispose of Mat before returning
					return true;
				}
			}

			frame.Dispose(); // Ensure cleanup before returning false
			return false;
		}


		private bool DetectIcon(Mat frame, Mat icon, out double matchValue)
		{
			matchValue = 0.0;

			try
			{
				var grayFrame = ConvertToGrayscale(frame);
				var frameEdges = ApplyCannyEdgeDetection(grayFrame);
				var iconEdges = ApplyCannyEdgeDetection(icon);

				bool result = PerformTemplateMatching(frameEdges, iconEdges, 0.40, out matchValue);

				// Explicitly dispose of resources after use
				grayFrame.Dispose();
				frameEdges.Dispose();
				iconEdges.Dispose();

				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during icon detection: {ex.Message}");
				return false;
			}
		}


		private Mat ConvertToGrayscale(Mat input)
		{
			if (input.Channels() == 1) return input.Clone();

			var grayscale = new Mat();
			Cv2.CvtColor(input, grayscale, ColorConversionCodes.BGR2GRAY);
			return grayscale;
		}

		private Mat ApplyCannyEdgeDetection(Mat input, double threshold1 = 100, double threshold2 = 200)
		{
			var edges = new Mat();
			Cv2.Canny(input, edges, threshold1, threshold2);
			return edges;
		}

		private bool PerformTemplateMatching(Mat frameEdges, Mat iconEdges, double threshold, out double matchValue)
		{
			var result = new Mat(); // Removed using statement

			Cv2.MatchTemplate(frameEdges, iconEdges, result, TemplateMatchModes.CCoeffNormed);
			Cv2.MinMaxLoc(result, out _, out matchValue, out _, out _);

			Console.WriteLine($"match: ${matchValue}, threshold: ${threshold}");

			bool isMatch = matchValue > threshold;

			result.Dispose(); // Explicitly dispose of Mat resource

			return isMatch;
		}

	}
}
