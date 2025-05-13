using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XeoClip2
{
	public partial class Form1 : Form
	{
		private bool isRecording = false;
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			
		}

		private void button1_Click(object sender, EventArgs e)
		{
			ToggleRecording();
		}
		//
		// Toggle button
		//
		private void ToggleRecording()
		{
			if (isRecording)
			{
				// Stop recording
				isRecording = false;
				button1.Text = "Start";
				// Stop the recording process
			}
			else
			{
				// Start recording
				isRecording = true;
				button1.Text = "Stop";
				// Start the recording process
				string ffPath = GetFFmpegPath();
			}
		}
		//
		// Check for FFMPEG
		//
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
					return !string.IsNullOrEmpty(output) ? output : throw new FileNotFoundException("FFmpeg not found in PATH.");
				}
			}

			throw new FileNotFoundException("FFmpeg not found in PATH.");
		}
	}
}
