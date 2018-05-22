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
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SimpliTube
{
	public class YoutubeDLManager : ManagerBase
	{
		#region Fields and Properties



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
				StringBuilder output = await ExecuteProcess(path, "--version");
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
				await client.DownloadFileTaskAsync(url, path);
			}
		}

		#endregion
	}
}