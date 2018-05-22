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
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
		private readonly string systemArchitecture = Environment.Is64BitOperatingSystem && Environment.Is64BitProcess ? "win64" : "win32";

		/// <summary>
		/// Path to the work directory
		/// </summary>
		private readonly string workPath = @".\work";

		#endregion

		#region Constructor

		public MainWindow()
		{
			InitializeComponent();

			Loaded += MainWindow_Loaded;
		}

		#endregion

		#region Event Handlers

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			OutputManager outputManager = new OutputManager(Dispatcher, txtOutput);
			Progress<OutputMessage> progressIndicator = new Progress<OutputMessage>(outputManager.Append);

			await CheckAndUpdateDependencies(progressIndicator).ConfigureAwait(false);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Checks if the required dependencies exist and downloads or updates them
		/// </summary>
		/// <param name="progress"></param>
		private async Task CheckAndUpdateDependencies(IProgress<OutputMessage> progress)
		{
			// Make sure the folder exists
			if (!Directory.Exists(workPath))
			{
				Directory.CreateDirectory(workPath);

				progress.Report(new OutputMessage() { Text = "Created work directory." });
			}
			else
			{
				progress.Report(new OutputMessage() { Text = "Work directory found." });
			}

			// Check and update Youtube-DL
			Task yt = Task.Run(async () =>
			{
				string[] versions = await Task.WhenAll(GetLatestYoutubeDLLocalVersion(), GetLatestYoutubeDLReleaseVersion()).ConfigureAwait(false);

				if (string.IsNullOrEmpty(versions[0]) || (!string.IsNullOrEmpty(versions[1]) && !versions[0].Equals(versions[1])))
				{
					await DownloadYoutubeDL(versions[1]).ConfigureAwait(false);
				}
			});

			// Check and update FFmpeg
			Task ff = Task.Run(async () =>
			{
				string[] versions = await Task.WhenAll(GetLatestFFmpegLocalVersion(), GetLatestFFmpegNightlyVersion()).ConfigureAwait(false);

				if (string.IsNullOrEmpty(versions[0]) || (!string.IsNullOrEmpty(versions[1]) && !versions[0].Contains(versions[1].Split(new char[] { '-' })[1])))
				{
					await DownloadFFmpeg(versions[1]).ConfigureAwait(false);
				}
			});

			await Task.WhenAll(yt, ff).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the existing version of Youtube-DL
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetLatestYoutubeDLLocalVersion()
		{
			string path = Path.Combine(workPath, "youtube-dl.exe");

			if (File.Exists(path))
			{
				StringBuilder output = await ExecuteProcess(path, "--version").ConfigureAwait(false);
				return output.ToString().Trim();
			}

			return string.Empty;
		}

		/// <summary>
		/// Gets the latest released version of Youtube-DL
		/// </summary>
		/// <returns></returns>
		private Task<string> GetLatestYoutubeDLReleaseVersion()
		{
			const string url = "https://github.com/rg3/youtube-dl/releases.atom";

			SyndicationFeed feed = null;

			using (XmlReader reader = XmlReader.Create(url))
			{
				feed = SyndicationFeed.Load(reader);
			}

			return feed == null ? Task.FromResult(string.Empty) :
				Task.FromResult(feed.Items.OrderByDescending(i => i.LastUpdatedTime).First().Title.Text.Split(new char[] { ' ' })[1]);
		}

		/// <summary>
		/// Downloads the latest released version of Youtube-DL
		/// </summary>
		/// <param name="version"></param>
		private async Task DownloadYoutubeDL(string version)
		{
			string url = "https://github.com/rg3/youtube-dl/releases/download/" + version + "/youtube-dl.exe";
			string path = Path.Combine(workPath, "youtube-dl.exe");

			using (WebClient client = new WebClient())
			{
				await client.DownloadFileTaskAsync(url, path).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Gets the existing version of FFmpeg
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetLatestFFmpegLocalVersion()
		{
			string path = Path.Combine(workPath, "ffmpeg.exe");

			if (File.Exists(path))
			{
				StringBuilder output = await ExecuteProcess(path, "-version").ConfigureAwait(false);
				string fullVersion = output.ToString().Substring(15);
				return fullVersion.Substring(0, fullVersion.IndexOf(" Copyright (c)")).Trim();
			}

			return string.Empty;
		}

		/// <summary>
		/// Gets the latest nightly version of FFmpeg
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetLatestFFmpegNightlyVersion()
		{
			const string url = "https://ffmpeg.zeranoe.com/builds/";

			using (WebClient client = new WebClient())
			{
				using (StreamReader reader = new StreamReader(client.OpenRead(url)))
				{
					string pageContent = await reader.ReadToEndAsync().ConfigureAwait(false);

					Regex regex = new Regex("(\\<input.*name=\"v\").*(value=\".*\")");
					Match match = regex.Match(pageContent);

					if (match.Success)
					{
						return match.Groups[2].Value.Substring(7, match.Groups[2].Value.Length - 8);
					}
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Downloads the latest released version of FFmpeg
		/// </summary>
		/// <param name="version"></param>
		private async Task DownloadFFmpeg(string version)
		{
			string fileName = "ffmpeg-" + version + "-" + systemArchitecture + "-static";
			string url = "https://ffmpeg.zeranoe.com/builds/" + systemArchitecture + "/static/" + fileName + ".zip";
			string zipPath = workPath + "/" + fileName + ".zip";

			using (WebClient client = new WebClient())
			{
				await client.DownloadFileTaskAsync(url, zipPath).ConfigureAwait(false);
			}

			ZipFile.ExtractToDirectory(zipPath, workPath);
			File.Delete(zipPath);

			File.Copy(workPath + "/" + fileName + "/bin/ffmpeg.exe", workPath + "/ffmpeg.exe", true);
			Directory.Delete(workPath + "/" + fileName, true);
		}

		/// <summary>
		/// Executes a process in the background and returns it's output
		/// </summary>
		/// <param name="path"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		private async Task<StringBuilder> ExecuteProcess(string path, string arguments)
		{
			TaskCompletionSource<StringBuilder> tcsProcess = new TaskCompletionSource<StringBuilder>();
			TaskCompletionSource<StringBuilder> tcsOutput = new TaskCompletionSource<StringBuilder>();
			StringBuilder output = new StringBuilder();

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
						tcsOutput.SetResult(output);
					}
					else
					{
						output.AppendLine(e.Data);
					}
				};

				process.Exited += async (sender, e) =>
				{
					tcsProcess.TrySetResult(await tcsOutput.Task);
				};

				process.Start();
				process.BeginOutputReadLine();

				return await tcsProcess.Task;
			}
		}

		#endregion
	}
}