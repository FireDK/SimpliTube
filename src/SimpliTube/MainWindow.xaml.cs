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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

		private OutputManager outputManager;
		private Progress<OutputMessage> progressIndicator;

		private YoutubeDLManager youtubeDLManager;
		private FFmpegManager ffmpegManager;

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

			outputManager = new OutputManager(Dispatcher, txtOutput);
			progressIndicator = new Progress<OutputMessage>(outputManager.Append);

			youtubeDLManager = new YoutubeDLManager(systemArchitecture, workPath, progressIndicator);
			ffmpegManager = new FFmpegManager(systemArchitecture, workPath, progressIndicator);

			outputManager.Append(new OutputMessage() { Text = "Checking for updates..." });

			await Task.WhenAll(
				youtubeDLManager.CheckForUpdates(),
				ffmpegManager.CheckForUpdates()
			);

			outputManager.Append(new OutputMessage() { Text = "Finished checking for updates." + Environment.NewLine });
		}

		private void lstFormats_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ListView listView = sender as ListView;
			GridView gridView = listView.View as GridView;

			var actualWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth - 10;

			for (int i = 0; i < gridView.Columns.Count - 1; i++)
			{
				actualWidth -= gridView.Columns[i].ActualWidth;
			}

			gridView.Columns[gridView.Columns.Count - 1].Width = actualWidth;
		}

		private async void btnLoadFormats_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(txtInput.Text))
			{
				outputManager.Append(new OutputMessage() { Text = "Loading available formats..." });

				lstFormats.ItemsSource = await youtubeDLManager.GetFormats(txtInput.Text);
			}
		}

		private async void btnDownload_Click(object sender, RoutedEventArgs e)
		{
			if (lstFormats.SelectedItems.Count > 0)
			{
				outputManager.Append(new OutputMessage() { Text = "Downloading selected items..." });

				List<YoutubeDLFormat> selectedFormats = new List<YoutubeDLFormat>();

				foreach (var item in lstFormats.SelectedItems)
				{
					selectedFormats.Add((YoutubeDLFormat)item);
				}

				await youtubeDLManager.Download(txtInput.Text, txtOutputDir.Text, selectedFormats);
			}
		}

		#endregion

		#region Private Methods



		#endregion
	}
}