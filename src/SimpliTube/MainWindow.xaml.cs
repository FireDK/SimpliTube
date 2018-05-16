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
		private string systemArchitecture = Environment.Is64BitOperatingSystem && Environment.Is64BitProcess ? "x86_64" : "i686";

		/// <summary>
		/// Path to store the dependencies
		/// </summary>
		private string dependenciesPath = @".\Dependencies";

		/// <summary>
		/// Path for Youtube-DL
		/// </summary>
		private string youtubeDLPath = @".\Dependencies\youtube-dl.exe";

		/// <summary>
		/// Path for FFmpeg
		/// </summary>
		private string ffmpegPath = @".\Dependencies\ffmpeg.exe";

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
			await CheckAndUpdateDependencies();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Checks if the required dependencies exist and downloads or updates them
		/// </summary>
		private async Task CheckAndUpdateDependencies()
		{
			// Make sure the folder exists
			if (!Directory.Exists(dependenciesPath))
			{
				Directory.CreateDirectory(dependenciesPath);
			}

			// Check and update Youtube-DL
			Task yt = Task.Run(async () =>
			{
				Task<string> yt1 = Task.Run(() => GetLatestYoutubeDLLocalVersion());
				Task<string> yt2 = Task.Run(() => GetLatestYoutubeDLReleaseVersion());

				string[] versions = await Task.WhenAll(yt1, yt2);

				if (string.IsNullOrEmpty(versions[0]) || (!string.IsNullOrEmpty(versions[1]) && !versions[0].Equals(versions[1])))
				{
					await DownloadYoutubeDL(versions[1]);
				}
			});

			// Check and update FFmpeg
			Task ff = Task.Run(async () =>
			{
				Task<string> ff1 = Task.Run(() => GetLatestFFmpegLocalVersion());
				Task<string> ff2 = Task.Run(() => GetLatestFFmpegNightlyVersion());

				string[] versions = await Task.WhenAll(ff1, ff2);

				if (string.IsNullOrEmpty(versions[0]) || (!string.IsNullOrEmpty(versions[1]) && !versions[0].Contains(versions[1].Split(new char[] { '-' })[1])))
				{
					await DownloadFFmpeg(versions[1]);
				}
			});

			await Task.WhenAll(yt, ff);
		}

		/// <summary>
		/// Gets the existing version of Youtube-DL
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetLatestYoutubeDLLocalVersion()
		{
			if (File.Exists(youtubeDLPath))
			{
				StringBuilder output = await ExecuteProcess(youtubeDLPath, "--version");

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
			string url = @"https://github.com/rg3/youtube-dl/releases.atom";

			SyndicationFeed feed = null;

			using (XmlReader reader = XmlReader.Create(url))
			{
				feed = SyndicationFeed.Load(reader);
			}

			return feed != null ? Task.FromResult(feed.Items.OrderByDescending(i => i.LastUpdatedTime).FirstOrDefault().Title.Text.Split(new char[] { ' ' })[1]) : Task.FromResult(string.Empty);
		}

		/// <summary>
		/// Downloads the latest released version of Youtube-DL
		/// </summary>
		/// <param name="version"></param>
		private async Task DownloadYoutubeDL(string version)
		{
			string fileName = @"/youtube-dl.exe";
			string url = @"https://github.com/rg3/youtube-dl/releases/download/" + version + fileName;

			using (WebClient client = new WebClient())
			{
				await client.DownloadFileTaskAsync(url, dependenciesPath + fileName);
			}
		}

		/// <summary>
		/// Gets the existing version of FFmpeg
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetLatestFFmpegLocalVersion()
		{
			if (File.Exists(ffmpegPath))
			{
				StringBuilder output = await ExecuteProcess(ffmpegPath, "-version");
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
			string url = @"https://ffmpeg.zeranoe.com/builds/";

			using (WebClient client = new WebClient())
			{
				using (StreamReader reader = new StreamReader(client.OpenRead(url)))
				{
					string pageContent = await reader.ReadToEndAsync();

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
			string arch = systemArchitecture == "x86_64" ? "win64" : "win32";
			string fileName = @"/ffmpeg-" + version + "-" + arch + @"-static.zip";
			string url = @"https://ffmpeg.zeranoe.com/builds/" + arch + @"/static" + fileName;

			using (WebClient client = new WebClient())
			{
				await client.DownloadFileTaskAsync(url, dependenciesPath + fileName);
			}

			ZipFile.ExtractToDirectory(dependenciesPath + fileName, dependenciesPath);
			File.Delete(dependenciesPath + fileName);

			File.Copy(dependenciesPath + fileName.Substring(0, fileName.Length - 4) + @"/bin/ffmpeg.exe", dependenciesPath + @"/ffmpeg.exe", true);
			Directory.Delete(dependenciesPath + fileName.Substring(0, fileName.Length - 4), true);
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