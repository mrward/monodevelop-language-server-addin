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

using System.Collections.Generic;
using Xwt;

namespace LanguageServer.UI
{
	partial class MainWindow : Window
	{
		Notebook notebook;
		RichTextView loggingTextView;
		Button clearLoggingTextButton;
		ComboBox messagingComboBox;
		TextEntry messagingTextEntry;
		Button sendLogMessageButton;
		Button showMessageButton;
		Button showMessageRequestButton;
		TextEntry responseTextEntry;
		List<DiagnosticWidget> diagnosticWidgets = new List<DiagnosticWidget> ();
		Button sendDiagnosticsButton;
		Button addDiagnosticButton;
		Button clearDiagnosticsButton;
		VBox diagnosticWidgetsVBox;
		TextEntry customMessageTextEntry;
		RichTextView settingsTextView;

		void Build ()
		{
			Title = "Language Server";
			Width = 720;
			Height = 480;

			notebook = new Notebook ();
			Content = notebook;

			AddLoggingTab ();
			AddDiagnosticsTab ();
			AddMessagingTab ();
			AddSettingsTab ();
			AddCustomTab ();
		}

		void AddCustomTab ()
		{
			var mainVBox = new VBox ();
			mainVBox.Margin = 10;
			notebook.Add (mainVBox, "Custom");

			var customHBox = new HBox ();
			mainVBox.PackStart (customHBox, false);

			var label = new Label ();
			label.Text = "Custom Text:";
			customHBox.PackStart (label);

			customMessageTextEntry = new TextEntry ();
			customHBox.PackStart (customMessageTextEntry, true);
		}

		void AddSettingsTab ()
		{
			var settingsVBox = new VBox ();
			notebook.Add (settingsVBox, "Settings");

			settingsTextView = new RichTextView ();
			settingsTextView.ReadOnly = true;
			settingsVBox.PackStart (settingsTextView, true, true);
		}

		void AddMessagingTab ()
		{
			var mainHBox = new HBox ();
			mainHBox.Margin = 10;
			notebook.Add (mainHBox, "Messaging");

			var leftVBox = new VBox ();
			mainHBox.PackStart (leftVBox, true, true);

			var topHBox = new HBox ();
			leftVBox.PackStart (topHBox);

			var label = new Label ();
			label.Text = "Text:";

			topHBox.PackStart (label);

			messagingTextEntry = new TextEntry ();
			topHBox.PackStart (messagingTextEntry, true);

			messagingComboBox = new ComboBox ();
			topHBox.PackStart (messagingComboBox, false, hpos: WidgetPlacement.End);

			var responseLabel = new Label ();
			responseLabel.Text = "Response from show message request:";
			leftVBox.PackStart (responseLabel);

			responseTextEntry = new TextEntry ();
			leftVBox.PackStart (responseTextEntry, true, vpos: WidgetPlacement.Start);

			var rightVBox = new VBox ();
			rightVBox.MarginLeft = 10;
			mainHBox.PackStart (rightVBox);

			sendLogMessageButton = new Button ();
			sendLogMessageButton.Label = "Log message";
			rightVBox.PackStart (sendLogMessageButton);

			showMessageButton = new Button ();
			showMessageButton.Label = "Show message";
			rightVBox.PackStart (showMessageButton);

			showMessageRequestButton = new Button ();
			showMessageRequestButton.Label = "Show request message";
			rightVBox.PackStart (showMessageRequestButton);
		}

		void AddDiagnosticsTab ()
		{
			var mainHBox = new HBox ();
			mainHBox.Margin = 10;
			notebook.Add (mainHBox, "Diagnostics");

			diagnosticWidgetsVBox = new VBox ();

			var scrollView = new ScrollView ();
			scrollView.ExpandVertical = true;
			scrollView.ExpandHorizontal = true;
			scrollView.Content = diagnosticWidgetsVBox;
			scrollView.VerticalScrollPolicy = ScrollPolicy.Always;
			scrollView.HorizontalScrollPolicy = ScrollPolicy.Never;

			mainHBox.PackStart (scrollView, true, true);

			var rightVBox = new VBox ();
			rightVBox.MarginLeft = 10;
			mainHBox.PackStart (rightVBox);

			sendDiagnosticsButton = new Button ();
			sendDiagnosticsButton.Label = "Send Diagnostics";
			rightVBox.PackStart (sendDiagnosticsButton);

			addDiagnosticButton = new Button ();
			addDiagnosticButton.Label = "Add";
			rightVBox.PackStart (addDiagnosticButton);

			clearDiagnosticsButton = new Button ();
			clearDiagnosticsButton.Label = "Clear";
			rightVBox.PackStart (clearDiagnosticsButton);
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
