﻿//
// StatusBarExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2017 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.LanguageServer.Client
{
	static class StatusBarExtensions
	{
		public static void ShowMessage (this StatusBar statusBar, LogMessageParams message)
		{
			if (string.IsNullOrEmpty (message.Message)) {
				return;
			}

			Runtime.RunInMainThread (() => {
				IconId icon = GetIcon (message);
				statusBar.ShowMessage (icon, message.Message);
				statusBar.SetMessageSourcePad (LanguageClientOutputPad.Pad);
			});
		}

		static IconId GetIcon (LogMessageParams message)
		{
			switch (message.MessageType) {
				case MessageType.Error:
					return Stock.StatusError;
				case MessageType.Warning:
					return Stock.StatusWarning;
				default:
					return null;
			}
		}
	}
}