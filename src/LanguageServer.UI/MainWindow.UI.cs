//
// MainWindow.UI.cs
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

using Xwt;

namespace LanguageServer.UI
{
	partial class MainWindow : Window
	{
		Notebook notebook;
		RichTextView loggingTextView;
		Button clearLoggingTextButton;

		void Build ()
		{
			Title = "Language Server";
			Width = 720;
			Height = 480;

			notebook = new Notebook ();
			Content = notebook;

			AddLoggingTab ();
			//AddDiagnosticsTab ();
			//AddMessagingTab ();
			//AddSettingsTab ();
			//AddCustomTab ();
		}

		void AddCustomTab ()
		{
			var customVBox = new VBox ();
			notebook.Add (customVBox, "Custom");
		}

		void AddSettingsTab ()
		{
			var settingsVBox = new VBox ();
			notebook.Add (settingsVBox, "Settings");
		}

		void AddMessagingTab ()
		{
			var messagingVBox = new VBox ();
			notebook.Add (messagingVBox, "Messaging");
		}

		void AddDiagnosticsTab ()
		{
			var diagnosticsVBox = new VBox ();
			notebook.Add (diagnosticsVBox, "Diagnostics");
		}

		void AddLoggingTab ()
		{
			var loggingVBox = new VBox ();
			notebook.Add (loggingVBox, "Logging");

			loggingTextView = new RichTextView ();
			loggingTextView.ReadOnly = true;

			var scrollView = new ScrollView ();
			scrollView.ExpandVertical = true;
			scrollView.ExpandHorizontal = true;
			scrollView.Content = loggingTextView;
			scrollView.VerticalScrollPolicy = ScrollPolicy.Always;
			scrollView.HorizontalScrollPolicy = ScrollPolicy.Automatic;

			loggingVBox.PackStart (scrollView, true, true);

			clearLoggingTextButton = new Button ("Clear");
			loggingVBox.PackStart (clearLoggingTextButton, false, hpos: WidgetPlacement.End);
		}
	}
}
