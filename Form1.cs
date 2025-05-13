using System;
using System.Windows.Forms;

namespace XeoClip2
{
	public partial class Form1 : Form
	{
		private Recorder recorder;
		private FileManager fileManager;

		public Form1()
		{
			InitializeComponent();
			fileManager = new FileManager();
			recorder = new Recorder();
			recorder.RecordingStatusChanged += Recorder_RecordingStatusChanged;
		}

		private void Recorder_RecordingStatusChanged(string message)
		{
			Invoke(new Action(() => toolStripStatusLabel2.Text = message));
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// No setup required as FileManager initializes the folder
			toolStripStatusLabel2.Text = "Awaiting command..";
		}

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				if (recorder.IsRecording)
				{
					recorder.StopRecording();
					button1.Text = "Start";
				}
				else
				{
					string outputFile = recorder.StartRecording(fileManager.BaseFolder);
					button1.Text = "Stop";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			try
			{
				fileManager.OpenBaseFolder();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void toolStripStatusLabel1_Click(object sender, EventArgs e)
		{

		}

		private void button3_Click(object sender, EventArgs e)
		{
			FileManager manager = new FileManager();
			manager.OpenFolder(manager.IconsFolder);
			manager.StartWatchingIconsFolder();
		}
	}
}