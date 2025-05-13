using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace XeoClip2
{
	public partial class Form1 : Form
	{
		private bool isRecording = false;
		private Process ffmpegProcess;
		private string baseFolder;
		private string outputFolder;
		private string outputFile;

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			string tempPath = Path.GetTempPath();
			baseFolder = Path.Combine(tempPath, "XeoClip2");
			Directory.CreateDirectory(baseFolder);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			ToggleRecording();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (Directory.Exists(baseFolder))
			{
				Process.Start("explorer.exe", baseFolder);
			}
			else
			{
				MessageBox.Show("The base folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ToggleRecording()
		{
			if (isRecording)
			{
				StopRecording();
			}
			else
			{
				StartRecording();
			}
		}

		private void StartRecording()
		{
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			outputFolder = Path.Combine(baseFolder, timestamp);

			try
			{
				Directory.CreateDirectory(outputFolder);
				if (!Directory.Exists(outputFolder))
				{
					throw new IOException("Failed to create output folder.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error creating folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			isRecording = true;
			button1.Text = "Stop";

			string ffmpegPath = GetFFmpegPath();
			if (string.IsNullOrEmpty(ffmpegPath))
			{
				MessageBox.Show("FFmpeg not found. Please ensure it is installed and in the PATH.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				isRecording = false;
				button1.Text = "Start";
				return;
			}

			outputFile = Path.Combine(outputFolder, "recording.mp4");

			string ffmpegArgs = GetFFmpegCommand(outputFile);

			var startInfo = new ProcessStartInfo
			{
				FileName = ffmpegPath,
				Arguments = ffmpegArgs,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				UseShellExecute = true, // Open in a new console window
				CreateNoWindow = false
			};

			try
			{
				ffmpegProcess = new Process { StartInfo = startInfo };
				ffmpegProcess.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to start recording: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				isRecording = false;
				button1.Text = "Start";
			}
		}

		private void StopRecording()
		{
			isRecording = false;
			button1.Text = "Start";

			if (ffmpegProcess != null && !ffmpegProcess.HasExited)
			{
				try
				{
					ffmpegProcess.Kill();
					ffmpegProcess.WaitForExit();
					ffmpegProcess.Dispose();
					MessageBox.Show($"Recording saved to: {outputFile}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Failed to stop recording: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private string GetFFmpegPath()
		{
			var ffmpegPath = "ffmpeg";
			var processStartInfo = new ProcessStartInfo
			{
				FileName = "where",
				Arguments = ffmpegPath,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (var process = Process.Start(processStartInfo))
			{
				if (process != null)
				{
					var output = process.StandardOutput.ReadToEnd().Trim();
					process.WaitForExit();
					var paths = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					return paths.Length > 0 ? paths[0] : null;
				}
			}

			return null;
		}

		private string GetFFmpegCommand(string outputFile)
			=> $"-hwaccel cuda -loglevel error -nostats -hide_banner -f gdigrab " +
			   $"-framerate 60 -video_size 1920x1080 -offset_x 0 -offset_y 0 -rtbufsize 100M " +
			   $"-i desktop -c:v h264_nvenc -preset p1 -pix_fmt yuv420p -rc:v vbr_hq -cq:v 21 " +
			   $"-b:v 8M -maxrate:v 16M -bufsize:v 32M -vsync cfr -f mp4 \"{outputFile}\"";
	}
}