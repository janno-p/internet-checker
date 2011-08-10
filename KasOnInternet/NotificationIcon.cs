/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 9.08.2011
 * Time: 19:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace KasOnInternet
{
	public sealed class NotificationIcon
	{
		private NotifyIcon notifyIcon;
		private ContextMenu notificationMenu;
		
		private bool? isConnected;
		private bool done = false;
		private bool forceCheck = false;
		
		private readonly Thread thread;
		
		private readonly Icon connectedIcon;
		private readonly Icon disconnectedIcon;
		private readonly Icon workingIcon;
		
		private readonly SoundPlayer connectedSound;
		private readonly SoundPlayer disconnectedSound;
		
		public NotificationIcon()
		{
			notifyIcon = new NotifyIcon();
			notificationMenu = new ContextMenu(new[]
			{
				new MenuItem("About", MenuAboutClick),
				new MenuItem("Exit", MenuExitClick)
			});
			
			notifyIcon.DoubleClick += IconDoubleClick;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon));
			notifyIcon.Icon = (Icon)resources.GetObject("$this.Icon");
			notifyIcon.ContextMenu = notificationMenu;
			
			connectedIcon = (Icon)resources.GetObject("$this.Connected");
			disconnectedIcon = (Icon)resources.GetObject("$this.Disconnected");
			workingIcon = (Icon)resources.GetObject("$this.Working");
			
			var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			
			connectedSound = new SoundPlayer(string.Format("{0}\\Audio\\Connected.wav", directory));
			connectedSound.Load();
			disconnectedSound = new SoundPlayer(string.Format("{0}\\Audio\\Disconnected.wav", directory));
			disconnectedSound.Load();
			
			thread = new Thread(CheckConnection);
			thread.Start();
		}
		
		#region Main - Program entry point
		/// <summary>Program entry point.</summary>
		/// <param name="args">Command Line Arguments</param>
		[STAThread]
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			bool isFirstInstance;
			// Please use a unique name for the mutex to prevent conflicts with other programs
			using (Mutex mtx = new Mutex(true, "KasOnInternet", out isFirstInstance)) {
				if (isFirstInstance) {
					NotificationIcon notificationIcon = new NotificationIcon();
					notificationIcon.notifyIcon.Visible = true;
					Application.Run();
					notificationIcon.notifyIcon.Dispose();
				} else {
					// The application is already running
					// TODO: Display message box or change focus to existing application instance
				}
			} // releases the Mutex
		}
		#endregion
		
		private void MenuAboutClick(object sender, EventArgs e)
		{
			MessageBox.Show("About This Application");
		}
		
		private void MenuExitClick(object sender, EventArgs e)
		{
			done = true;
			thread.Join();
			Application.Exit();
		}
		
		private void IconDoubleClick(object sender, EventArgs e)
		{
			forceCheck = true;
		}
		
		private void Wait()
		{
			const int sleepTime = 60000;
			const int step = 1000;
			
			var sleptTime = 0;
			while (!done && !forceCheck && sleptTime < sleepTime)
			{
				sleptTime += step;
				Thread.Sleep(step);
			}
			
			forceCheck = false;
		}
		
		private void CheckConnection()
		{
			try
			{
				while (!done)
				{
					notifyIcon.Icon = workingIcon;
					notifyIcon.Text = "Kontrollin ühendust ...";
					
					var connectionStatus = ConnectionExists();
					var icon = connectionStatus ? connectedIcon : disconnectedIcon;
					var message = connectionStatus ? "Ühendus on olemas!" : "Ühendus puudub!";
					var sound = connectionStatus ? connectedSound : disconnectedSound;
					
					notifyIcon.Icon = icon;
					notifyIcon.Text = message;
					
					if (!isConnected.HasValue || connectionStatus != isConnected.Value)
					{
						notifyIcon.ShowBalloonTip(60000, "Interneti ühendus", message, ToolTipIcon.None);
						sound.Play();
					}
					isConnected = connectionStatus;
					
					Wait();
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.ToString());
				Application.Exit();
			}
		}
		
		private static bool ConnectionExists()
		{
			try
			{
				Dns.GetHostEntry("www.google.com");
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
