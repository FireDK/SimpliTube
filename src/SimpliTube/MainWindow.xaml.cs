/*
	SimpliTube - An easy way of downloading and converting media files.
	Copyright (C) 2018 Octavian Bobocea
	Home: https://github.com/FireDK/SimpliTube

	This file is part of SimpliTube.

	SimpliTube is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	SimpliTube is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with SimpliTube. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml;

namespace SimpliTube
{
	public partial class MainWindow : Window
	{
		#region Fields and Properties

		/// <summary>
		/// Current system architecture
		/// </summary>
		private string systemArchitecture = Environment.Is64BitOperatingSystem && Environment.Is64BitProcess ? "x86_64" : "i686";

		/// <summary>
		/// Path to store the dependencies
		/// </summary>
		private string dependenciesPath = @".\Dependencies";

		/// <summary>
		/// Path for Youtube-DL
		/// </summary>
		private string youtubeDLPath = @".\Dependencies\youtube-dl.exe";

		#endregion

		#region Constructor

		public MainWindow()
		{
			InitializeComponent();

			Loaded += MainWindow_Loaded;
		}

		#endregion

		#region Event Handlers

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			CheckAndUpdateDependencies();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Checks if the required dependencies exist and downloads or updates them
		/// </summary>
		private void CheckAndUpdateDependencies()
		{
			// Make sure the folder exists
			if (!Directory.Exists(dependenciesPath))
			{
				Directory.CreateDirectory(dependenciesPath);
			}

			// Youtube-DL
			string latestYoutubeDLLocal = GetLatestYoutubeDLLocalVersion();
			string latestYoutubeDLRelease = GetLatestYoutubeDLReleaseVersion();

			if (string.IsNullOrEmpty(latestYoutubeDLLocal) || (!latestYoutubeDLLocal.Equals(latestYoutubeDLRelease) && !string.IsNullOrEmpty(latestYoutubeDLRelease)))
			{
				DownloadYoutubeDL(latestYoutubeDLRelease);
			}
		}

		/// <summary>
		/// Gets the existing version of Youtube-DL
		/// </summary>
		/// <returns></returns>
		private string GetLatestYoutubeDLLocalVersion()
		{
			if (File.Exists(youtubeDLPath))
			{
				return ExecuteProcess(youtubeDLPath, "--version").ToString().Trim();
			}

			return string.Empty;
		}

		/// <summary>
		/// Gets the latest released version of Youtube-DL
		/// </summary>
		/// <returns></returns>
		private string GetLatestYoutubeDLReleaseVersion()
		{
			string url = @"https://github.com/rg3/youtube-dl/releases.atom";

			SyndicationFeed feed = GetFeed(url);

			if (feed != null)
			{
				SyndicationItem latestEntry = feed.Items.OrderByDescending(i => i.LastUpdatedTime).FirstOrDefault();

				return latestEntry.Title.Text.Split(new char[] { ' ' })[1];
			}

			return string.Empty;
		}

		/// <summary>
		/// Downloads the latest released version of Youtube-DL
		/// </summary>
		/// <param name="version"></param>
		private void DownloadYoutubeDL(string version)
		{
			string url = @"https://github.com/rg3/youtube-dl/releases/download/" + version + @"/youtube-dl.exe";

			using (WebClient client = new WebClient())
			{
				// TODO: Change this to async to not block the UI and find a way to either show progress or a wait panel.
				client.DownloadFile(url, youtubeDLPath);
			}
		}

		/// <summary>
		/// Loads a RSS feed
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private SyndicationFeed GetFeed(string url)
		{
			SyndicationFeed feed = null;

			using (XmlReader reader = XmlReader.Create(url))
			{
				feed = SyndicationFeed.Load(reader);
			}

			return feed;
		}

		/// <summary>
		/// Executes a process in the background and returns it's output
		/// </summary>
		/// <param name="path"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		private StringBuilder ExecuteProcess(string path, string arguments)
		{
			// Uses code adapted from https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
			StringBuilder output = new StringBuilder();

			using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
			{
				using (Process process = new Process())
				{
					process.StartInfo.FileName = path;
					process.StartInfo.Arguments = arguments;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					process.StartInfo.CreateNoWindow = true;
					process.OutputDataReceived += (sender, e) =>
					{
						if (e.Data == null)
						{
							outputWaitHandle.Set();
						}
						else
						{
							output.AppendLine(e.Data);
						}
					};

					try
					{
						process.Start();

						process.BeginOutputReadLine();

						process.WaitForExit();
					}
					finally
					{
						outputWaitHandle.WaitOne();
					}
				}
			}

			return output;
		}

		#endregion
	}
}