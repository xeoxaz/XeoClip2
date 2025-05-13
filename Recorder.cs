using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace XeoClip2
{
	public class Recorder : IDisposable
	{

		// Recording
		//
		private DateTime recordingStartTime;
		private DateTime recordingEndTime;
		//
		//
		//

		private Process ffmpegProcess;
		private bool isRecording;
		private string outputFile;

		public event Action<string> RecordingStatusChanged;

		IconWatcher iw = new IconWatcher();
		private string outputFolder;

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
			outputFolder = Path.Combine(baseFolder, timestamp);
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
				CreateNoWindow = true // no window "true"
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
				recordingStartTime = DateTime.Now; // start time

				ffmpegProcess.BeginOutputReadLine();
				ffmpegProcess.BeginErrorReadLine();
				ffmpegProcess.PriorityClass = ProcessPriorityClass.RealTime;

				isRecording = true;
				UpdateStatus("Recording started successfully.");
				Console.Beep(1300, 150);

				//
				// IW
				//
				iw.StartWatching(recordingStartTime);

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

			// Maybe?
			recordingEndTime = DateTime.Now;
			//
			// IW
			//
			iw.StopWatching();

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
				Console.Beep(1150, 150);

				// Check for timestamps from IconWatcher
				List<TimeSpan> timestamps = iw.GetBufferedTimestamps();
				string listFile = Path.Combine(outputFolder, "filelist.txt");

				if (timestamps.Count > 0)
				{
					Console.WriteLine($"-- Time_Stamps: {timestamps.Count}");
					StreamWriter sw = new StreamWriter(listFile);
					Process ffmpegProcess = null; // Centralized FFmpeg process handler

					foreach (TimeSpan timestamp in timestamps)
					{
						Console.WriteLine($"Debug Timestamp: {timestamp}");

						string clipFile = Path.Combine(outputFolder, $"clip_{timestamp.TotalSeconds:F3}.flv");


						TimeSpan recordingDuration = GetRecordingDuration();
						TimeSpan adjustedStartTime = timestamp - TimeSpan.FromSeconds(5);

						if (adjustedStartTime > recordingDuration - TimeSpan.FromSeconds(10))
						{
							if (recordingDuration.TotalSeconds > 10)
								adjustedStartTime = recordingDuration - TimeSpan.FromSeconds(10);
							else
								adjustedStartTime = TimeSpan.Zero; // If video is shorter than 10s, extract from the start
						}

						string startTime = adjustedStartTime.ToString(@"hh\:mm\:ss\.fff");



						Console.WriteLine("");
						Console.WriteLine("");
						Console.WriteLine($"Timestamp: {timestamp}, StartTime: {startTime}");

						string ffmpegCommand = $"ffmpeg -hide_banner -copyts -ss {startTime} -i \"{outputFile}\" -t 10 -c copy -f flv \"{clipFile}\"";
						Console.WriteLine($"FFmpeg Command: {ffmpegCommand}");


						Console.WriteLine($"Processing clip: {clipFile}");

						ffmpegProcess = CreateFFmpegProcess(ffmpegCommand);
						ffmpegProcess.Start();
						ffmpegProcess.WaitForExit();

						// Validate success before adding to list file
						if (ffmpegProcess.ExitCode == 0 && File.Exists(clipFile))
						{
							sw.WriteLine($"file '{clipFile}'");
						}
						else
						{
							Console.WriteLine($"Warning: FFmpeg failed for timestamp {timestamp}");
						}
					}

					sw.Close();
					ffmpegProcess?.Dispose(); // Dispose the FFmpeg instance after clipping

					//
					// Merge clips
					//
					//
					string mergeFile = Path.Combine(outputFolder, "merged_output.flv");
					string mergeCommand = $"ffmpeg -hide_banner -f concat -safe 0 -i \"{listFile}\" -c:v libx264 -c:a aac \"{mergeFile}\"";

					Console.WriteLine($"Merging clips into {mergeFile}...");

					ffmpegProcess = CreateFFmpegProcess(mergeCommand);
					ffmpegProcess.Start();
					ffmpegProcess.WaitForExit();
					ffmpegProcess.Dispose(); // Ensure FFmpeg process is properly closed

					Console.WriteLine("Merging completed!");
				}

				// Clear timestamps after processing
				iw.ClearBufferedTimestamps();
			}
			catch (Exception ex)
			{
				UpdateStatus($"Error during file validation: {ex.Message}");
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
			UpdateStatus("Configuring optimized FFmpeg parameters for FLV with updated options...");

			return $"-f dshow -rtbufsize 512M -i audio=\"virtual-audio-capturer\":video=\"screen-capture-recorder\" " +
				   $"-c:v h264_nvenc -preset p1 -pix_fmt yuv420p -rc:v vbr -cq:v 21 " +
				   $"-b:v 8M -maxrate:v 16M -bufsize:v 32M -fps_mode cfr " +
				   $"-c:a aac -b:a 128k -ar 44100 -ac 2 -f flv \"{outputFile}\"";
		}

		private Process CreateFFmpegProcess(string command)
		{
			Console.WriteLine("-- Create_Process: ");
			return new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/C {command}",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
		}

		private TimeSpan GetRecordingDuration()
		{
			return recordingEndTime - recordingStartTime;
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
