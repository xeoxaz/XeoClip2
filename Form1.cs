using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XeoClip2
{
	public partial class Form1 : Form
	{
		private Recorder recorder;
		private FileManager fileManager;

		//
		// Global hot key F6
		//
		private const int WM_HOTKEY = 0x0312;
		private const int MOD_NOREPEAT = 0x4000; // Prevents repeat firing
		private const int MOD_ALT = 0x0001; // Example modifier key
		private const int VK_F6 = 0x75;

		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
		//
		//
		//
		public Form1()
		{
			InitializeComponent();
			RegisterHotKey(this.Handle, 1, MOD_NOREPEAT, VK_F6);


			fileManager = new FileManager();
			recorder = new Recorder();
			recorder.RecordingStatusChanged += Recorder_RecordingStatusChanged;
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_HOTKEY)
			{
				int id = m.WParam.ToInt32();
				if (id == 1)
				{
					_= ToggleRecording();
				}
			}
			base.WndProc(ref m);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			UnregisterHotKey(this.Handle, 1);
			base.OnFormClosing(e);
		}

		private void Recorder_RecordingStatusChanged(string message)
		{
			if (InvokeRequired)
			{
				Invoke(new Action(() => toolStripStatusLabel2.Text = message));
			}
			else
			{
				toolStripStatusLabel2.Text = message;
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// No setup required as FileManager initializes the folder
			toolStripStatusLabel2.Text = "Awaiting command..";
		}

		private async void button1_Click(object sender, EventArgs e)
		{
			await ToggleRecording();
		}
		private async Task ToggleRecording()
		{
			try
			{
				if (recorder.IsRecording)
				{
					await recorder.StopRecordingAsync();
					UpdateUI(false);
				}
				else
				{
					string outputFile = recorder.StartRecording(fileManager.BaseFolder);
					UpdateUI(true);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void UpdateUI(bool isRecording)
		{
			if (InvokeRequired)
			{
				Invoke(new Action(() => button1.Text = isRecording ? "Stop" : "Start"));
			}
			else
			{
				button1.Text = isRecording ? "Stop" : "Start";
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
		}
	}
}