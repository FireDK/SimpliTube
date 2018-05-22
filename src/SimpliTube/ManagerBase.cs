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
using System.Text;
using System.Threading.Tasks;

namespace SimpliTube
{
	public class ManagerBase
	{
		#region Fields and Properties

		protected readonly string workPath;

		protected readonly string systemArchitecture;

		protected IProgress<OutputMessage> progressIndicator;

		#endregion

		#region Constructor

		public ManagerBase(string systemArchitecture, string workPath, IProgress<OutputMessage> progressIndicator)
		{
			this.systemArchitecture = systemArchitecture;
			this.workPath = workPath;
			this.progressIndicator = progressIndicator;
		}

		#endregion

		#region Public Methods



		#endregion

		#region Private Methods

		/// <summary>
		/// Executes a process in the background and returns it's output
		/// </summary>
		/// <param name="path"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		protected async Task<StringBuilder> ExecuteProcess(string path, string arguments)
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
				process.EnableRaisingEvents = true;

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
					tcsProcess.SetResult(await tcsOutput.Task);
				};

				process.Start();
				process.BeginOutputReadLine();

				return await tcsProcess.Task;
			}
		}

		#endregion
	}
}