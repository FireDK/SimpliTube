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
using System.Windows.Controls;
using System.Windows.Threading;

namespace SimpliTube
{
	public class OutputMessage
	{
		public string Text { get; set; }
	}

	public class OutputManager
	{
		#region Fields and Properties

		private readonly RichTextBox textBoxRef;

		private readonly Dispatcher dispatcherRef;

		#endregion

		#region Constructor

		public OutputManager(Dispatcher dispatcher, RichTextBox destination)
		{
			dispatcherRef = dispatcher;
			textBoxRef = destination;
		}

		#endregion

		#region Public Methods

		public void Append(OutputMessage message)
		{
			dispatcherRef.BeginInvoke((Action)(() =>
			{
				textBoxRef.AppendText(message.Text);
				textBoxRef.AppendText(Environment.NewLine);
			}));
		}

		#endregion
	}
}