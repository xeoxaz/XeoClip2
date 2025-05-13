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
		private string outputFolder;
		private string outputFile;

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// No folder creation on load
		}

		private void button1_Click(object sender, EventArgs e)
		{
			ToggleRecording();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			// Open the video storage folder
			if (Directory.Exists(outputFolder))
			{
				Process.Start("explorer.exe", outputFolder);
			}
			else
			{
				MessageBox.Show("The output folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
			// Create folder only when recording starts
			string tempPath = Path.GetTempPath();
			outputFolder = Path.Combine(tempPath, "XeoClip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

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

			// FFmpeg command to record desktop with audio
			string ffmpegArgs = $"-f gdigrab -framerate 30 -i desktop -f dshow -i audio=\"virtual-audio-capturer\" " +
								$"-c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p -c:a aac -b:a 128k \"{outputFile}\"";

			var startInfo = new ProcessStartInfo
			{
				FileName = ffmpegPath,
				Arguments = ffmpegArgs,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
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
			var ffmpegPath = "ffmpeg"; // Assuming "ffmpeg" is in the PATH
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
	}
}