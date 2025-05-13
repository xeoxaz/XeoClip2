using System;
using System.Diagnostics;
using System.IO;

namespace XeoClip2
{
	public class Recorder : IDisposable
	{
		private Process ffmpegProcess;
		private bool isRecording;
		private string outputFile;

		/// <summary>
		/// Starts the recording process and returns the output file path.
		/// </summary>
		/// <param name="baseFolder">Base folder where recordings will be stored.</param>
		/// <returns>Path of the output recording file.</returns>
		public string StartRecording(string baseFolder)
		{
			if (isRecording)
				throw new InvalidOperationException("Recording is already in progress.");

			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string outputFolder = Path.Combine(baseFolder, timestamp);
			Directory.CreateDirectory(outputFolder);

			outputFile = Path.Combine(outputFolder, "recording.mp4");

			string ffmpegPath = GetFFmpegPath();
			if (string.IsNullOrEmpty(ffmpegPath))
				throw new FileNotFoundException("FFmpeg not found. Ensure it is installed and accessible in the PATH.");

			string ffmpegArgs = GetFFmpegCommand(outputFile);

			var startInfo = new ProcessStartInfo
			{
				FileName = ffmpegPath,
				Arguments = ffmpegArgs,
				UseShellExecute = true,
				CreateNoWindow = false
			};

			try
			{
				ffmpegProcess = new Process { StartInfo = startInfo };
				ffmpegProcess.Start();
				isRecording = true;
				return outputFile;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to start recording.", ex);
			}
		}

		/// <summary>
		/// Stops the ongoing recording process.
		/// </summary>
		public void StopRecording()
		{
			if (!isRecording)
				throw new InvalidOperationException("No recording is in progress to stop.");

			if (ffmpegProcess != null && !ffmpegProcess.HasExited)
			{
				try
				{
					ffmpegProcess.Kill();
					ffmpegProcess.WaitForExit();
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException("Failed to stop recording.", ex);
				}
				finally
				{
					ffmpegProcess.Dispose();
					ffmpegProcess = null;
				}
			}

			isRecording = false;
		}

		/// <summary>
		/// Locates the FFmpeg executable path.
		/// </summary>
		/// <returns>Path to the FFmpeg executable.</returns>
		private string GetFFmpegPath()
		{
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
					return paths.Length > 0 ? paths[0] : null;
				}
			}

			return null;
		}

		/// <summary>
		/// Constructs the FFmpeg command arguments.
		/// </summary>
		/// <param name="outputFile">Output file path for the recording.</param>
		/// <returns>FFmpeg command arguments.</returns>
		private string GetFFmpegCommand(string outputFile)
		{
			return $"-hwaccel cuda -f gdigrab -framerate 60 -video_size 1920x1080 -i desktop -c:v h264_nvenc -preset p1 \"{outputFile}\"";
		}

		/// <summary>
		/// Disposes resources used by the recorder.
		/// </summary>
		public void Dispose()
		{
			if (ffmpegProcess != null)
			{
				if (!ffmpegProcess.HasExited)
				{
					ffmpegProcess.Kill();
					ffmpegProcess.WaitForExit();
				}

				ffmpegProcess.Dispose();
				ffmpegProcess = null;
			}
		}

		public bool IsRecording => isRecording;
	}
}