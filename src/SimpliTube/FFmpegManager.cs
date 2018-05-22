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
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpliTube
{
	public class FFmpegManager : ManagerBase
	{
		#region Fields and Properties



		#endregion

		#region Constructor

		public FFmpegManager(string systemArchitecture, string workPath, IProgress<OutputMessage> progressIndicator)
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
					GetLatestFFmpegLocalVersion(),
					GetLatestFFmpegNightlyVersion()
				);

				if (string.IsNullOrEmpty(versions[0]) || (!string.IsNullOrEmpty(versions[1]) && !versions[0].Contains(versions[1].Split(new char[] { '-' })[1])))
				{
					progressIndicator.Report(new OutputMessage() { Text = "Downloading the latest version of FFmpeg..." });

					await DownloadFFmpeg(versions[1]);

					progressIndicator.Report(new OutputMessage() { Text = "Finished downloading FFmpeg." });
				}
			});
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the existing version of FFmpeg
		/// </summary>
		/// <returns></returns>
		private async Task<string> GetLatestFFmpegLocalVersion()
		{
			string path = Path.Combine(workPath, "ffmpeg.exe");

			if (File.Exists(path))
			{
				StringBuilder output = await ExecuteProcess(path, "-version");
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
			string fileName = "ffmpeg-" + version + "-" + systemArchitecture + "-static";
			string url = "https://ffmpeg.zeranoe.com/builds/" + systemArchitecture + "/static/" + fileName + ".zip";
			string zipPath = workPath + "/" + fileName + ".zip";

			using (WebClient client = new WebClient())
			{
				await client.DownloadFileTaskAsync(url, zipPath);
			}

			ZipFile.ExtractToDirectory(zipPath, workPath);
			File.Delete(zipPath);

			File.Copy(workPath + "/" + fileName + "/bin/ffmpeg.exe", workPath + "/ffmpeg.exe", true);
			Directory.Delete(workPath + "/" + fileName, true);
		}

		#endregion
	}
}