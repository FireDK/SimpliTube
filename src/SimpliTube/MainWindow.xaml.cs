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
using System.Threading.Tasks;
using System.Windows;

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
			// Make sure the work directory exists
			if (!Directory.Exists(workPath))
			{
				Directory.CreateDirectory(workPath);
			}

			OutputManager outputManager = new OutputManager(Dispatcher, txtOutput);
			Progress<OutputMessage> progressIndicator = new Progress<OutputMessage>(outputManager.Append);
			YoutubeDLManager youtubeDLManager = new YoutubeDLManager(systemArchitecture, workPath, progressIndicator);
			FFmpegManager ffmpegManager = new FFmpegManager(systemArchitecture, workPath, progressIndicator);

			outputManager.Append(new OutputMessage() { Text = "Checking for updates..." });

			await Task.WhenAll(
				youtubeDLManager.CheckForUpdates(),
				ffmpegManager.CheckForUpdates()
			);

			outputManager.Append(new OutputMessage() { Text = "Finished checking for updates." + Environment.NewLine });
		}

		#endregion

		#region Private Methods



		#endregion
	}
}