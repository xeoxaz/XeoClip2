using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace XeoClip2
{
	public class Recorder : IDisposable
	{
		private Process ffmpegProcess;
		private bool isRecording;
		private string outputFile;

		public event Action<string> RecordingStatusChanged;

		private void UpdateStatus(string message)
		{
			RecordingStatusChanged?.Invoke(message);
		}

		public string StartRecording(string baseFolder)
		{
			if (isRecording)
				throw new InvalidOperationException("Recording is already in progress.");

			UpdateStatus("Initializing recording process...");

			// Generate timestamped output folder
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string outputFolder = Path.Combine(baseFolder, timestamp);
			Directory.CreateDirectory(outputFolder);

			outputFile = Path.Combine(outputFolder, "recording.flv");

			// Verify FFmpeg path
			string ffmpegPath = GetFFmpegPath();
			if (string.IsNullOrEmpty(ffmpegPath))
			{
				UpdateStatus("Error: FFmpeg not found.");
				throw new FileNotFoundException("FFmpeg executable not found. Ensure it's installed and accessible via PATH.");
			}

			// Prepare FFmpeg command arguments
			string ffmpegArgs = GetFFmpegCommand(outputFile);
			UpdateStatus($"FFmpeg command prepared:\n{ffmpegArgs}");

			var startInfo = new ProcessStartInfo
			{
				FileName = ffmpegPath,
				Arguments = ffmpegArgs,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = false
			};

			try
			{
				ffmpegProcess = new Process { StartInfo = startInfo };

				// Capture FFmpeg output & errors asynchronously
				ffmpegProcess.OutputDataReceived += (sender, args) =>
				{
					if (!string.IsNullOrEmpty(args.Data))
						UpdateStatus($"FFmpeg Output: {args.Data}");
						Console.WriteLine($"[FFmpeg Output] {args.Data}");
				};

				ffmpegProcess.ErrorDataReceived += (sender, args) =>
				{
					if (!string.IsNullOrEmpty(args.Data))
						UpdateStatus($"FFmpeg Error: {args.Data}");
						Console.WriteLine($"[FFmpeg Error] {args.Data}");
				};

				// Start FFmpeg recording
				ffmpegProcess.Start();
				ffmpegProcess.BeginOutputReadLine();
				ffmpegProcess.BeginErrorReadLine();
				ffmpegProcess.PriorityClass = ProcessPriorityClass.RealTime;

				isRecording = true;
				UpdateStatus("Recording started successfully.");
				return outputFile;
			}
			catch (Exception ex)
			{
				UpdateStatus($"Recording failed: {ex.Message}");
				throw new InvalidOperationException("Failed to start recording due to an unexpected error.", ex);
			}
		}

		public void StopRecording()
		{
			if (!isRecording)
				throw new InvalidOperationException("No recording in progress to stop.");

			UpdateStatus("Stopping recording, please wait...");

			Thread cleanupThread = new Thread(() =>
			{
				if (ffmpegProcess != null && !ffmpegProcess.HasExited)
				{
					try
					{
						UpdateStatus("Sending stop signal to FFmpeg...");
						ffmpegProcess.StandardInput.WriteLine("q");
						ffmpegProcess.StandardInput.Flush();

						ffmpegProcess.WaitForExit(5000);

						if (!ffmpegProcess.HasExited)
						{
							UpdateStatus("Forcing FFmpeg termination...");
							ffmpegProcess.Kill();
							ffmpegProcess.WaitForExit();
						}
					}
					catch (Exception ex)
					{
						UpdateStatus("Recording termination error. See logs.");
						throw new InvalidOperationException("Failed to stop recording.", ex);
					}
					finally
					{
						UpdateStatus("Cleaning up FFmpeg process...");
						ffmpegProcess.Dispose();
						ffmpegProcess = null;
					}
				}

				EnsureFileIsSaved();
				isRecording = false;

				UpdateStatus("Recording successfully stopped.");
			})
			{
				Priority = ThreadPriority.Highest
			};

			cleanupThread.Start();
		}

		private void EnsureFileIsSaved()
		{
			try
			{
				UpdateStatus("Validating recorded file...");

				if (string.IsNullOrEmpty(outputFile) || !File.Exists(outputFile))
				{
					UpdateStatus("Recording file missing, process failed.");
					throw new InvalidOperationException("Recording file was not saved properly.");
				}

				FileInfo fileInfo = new FileInfo(outputFile);
				if (fileInfo.Length == 0)
				{
					UpdateStatus("Warning: The recorded file is empty.");
					throw new InvalidOperationException("The recorded file is empty.");
				}

				UpdateStatus("Recording file saved successfully.");
			}
			catch (Exception ex)
			{
				UpdateStatus($"Error during file validation: {ex.Message}");
//				throw; // Re-throwing to ensure caller handles it appropriately
			}
		}

		private string GetFFmpegPath()
		{
			UpdateStatus("Locating FFmpeg executable...");
			var processStartInfo = new ProcessStartInfo
			{
				FileName = "where",
				Arguments = "ffmpeg",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (var process = Process.Start(processStartInfo))
			{
				if (process != null)
				{
					string output = process.StandardOutput.ReadToEnd().Trim();
					process.WaitForExit();
					string[] paths = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					string foundPath = paths.Length > 0 ? paths[0] : null;

					UpdateStatus(foundPath != null ? "FFmpeg found." : "FFmpeg not detected.");
					return foundPath;
				}
			}

			UpdateStatus("FFmpeg executable lookup failed.");
			return null;
		}

		private string GetFFmpegCommand(string outputFile)
		{
			UpdateStatus("Configuring FFmpeg with virtual-audio-capturer...");

			return $"-f dshow -i audio=\"virtual-audio-capturer\":video=\"screen-capture-recorder\" " +
				   $"-c:v libx264 -preset ultrafast -b:v 4000k -maxrate 4000k -bufsize 8000k -pix_fmt yuv420p " +
				   $"-c:a aac -b:a 128k -ar 44100 -ac 2 -f flv \"{outputFile}\"";
		}



		public void Dispose()
		{
			if (isRecording)
			{
				UpdateStatus("Disposing recorder and stopping any active processes...");
				StopRecording();
			}

			UpdateStatus("Final cleanup...");
			ffmpegProcess?.Dispose();
		}

		public bool IsRecording => isRecording;
	}
}
