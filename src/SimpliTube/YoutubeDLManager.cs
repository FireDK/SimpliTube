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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace SimpliTube
{
	public class YoutubeDLFormat
	{
		public string FormatCode { get; set; }
		public string Extension { get; set; }
		public string Resolution { get; set; }
		public string Note { get; set; }
	}

	public class YoutubeDLManager : ManagerBase
	{
		#region Fields and Properties

		private readonly string defaultOptions = "--ignore-config --no-color ";

		#endregion

		#region Constructor

		public YoutubeDLManager(string systemArchitecture, string workPath, IProgress<OutputMessage> progressIndicator)
			: base(systemArchitecture, workPath, progressIndicator)
		{
		}

		#endregion

		#region Public Methods

		public async Task CheckForUpdates()
		{
			await Task.Run(async () =>
			{
				string[] versions = await Task.WhenAll(
					GetLatestYoutubeDLLocalVersion(),
					GetLatestYoutubeDLReleaseVersion()
				);

				if (string.IsNullOrEmpty(versions[0]) || (!string.IsNullOrEmpty(versions[1]) && !versions[0].Equals(versions[1])))
				{
					progressIndicator.Report(new OutputMessage() { Text = "Downloading the latest version of YoutubeDL..." });

					await DownloadYoutubeDL(versions[1]);

					progressIndicator.Report(new OutputMessage() { Text = "Finished downloading YoutubeDL." });
				}
			});
		}

		public async Task<List<YoutubeDLFormat>> GetFormats(string url)
		{
			string path = Path.Combine(workPath, "youtube-dl.exe");
			List<YoutubeDLFormat> result = new List<YoutubeDLFormat>();

			if (File.Exists(path))
			{
				List<string> output = await ExecuteProcess(path, defaultOptions + "--list-formats " + url);
				int index = output.FindIndex(i => i.StartsWith("format code")) + 1;

				for (int i = index; i < output.Count; i++)
				{
					Regex regex = new Regex(@"^([\w-]*)\s*([\w-]*)\s*(audio only|[\w-]*)\s*(.*)$"); // Let's hope this works correctly
					Match match = regex.Match(output[i]);

					if (match.Success)
					{
						result.Add(new YoutubeDLFormat()
						{
							FormatCode = match.Groups[1].ToString(),
							Extension = match.Groups[2].ToString(),
							Resolution = match.Groups[3].ToString(),
							Note = match.Groups[4].ToString()
						});
					}
				}
			}

			return result;
		}

		public async Task Download(string url, string outputDir, List<YoutubeDLFormat> formats)
		{
			string path = Path.Combine(workPath, "youtube-dl.exe");

			if (File.Exists(path))
			{
				foreach (YoutubeDLFormat format in formats)
				{
					List<string> output = await ExecuteProcess(path, defaultOptions + "--format " + format.FormatCode +
						" --output \"" + outputDir + "/%(title)s.%(ext)s\" --restrict-filenames " + url);

					progressIndicator.Report(new OutputMessage() { Text = string.Join(Environment.NewLine, output) });
				}
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the existing version of Youtube-DL
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetLatestYoutubeDLLocalVersion()
		{
			string path = Path.Combine(workPath, "youtube-dl.exe");

			if (File.Exists(path))
			{
				List<string> output = await ExecuteProcess(path, defaultOptions + "--version");
				return output[0].Trim();
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
				await client.DownloadFileTaskAsync(url, path);
			}
		}

		#endregion
	}
}