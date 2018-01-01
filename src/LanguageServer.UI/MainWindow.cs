//
// MainWindow.cs
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

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xwt;
using Xwt.Formats;

namespace LanguageServer.UI
{
	partial class MainWindow : ILogger
	{
		MainWindowViewModel viewModel;

		public MainWindow ()
		{
			LoggingService.Logger = this;

			viewModel = new MainWindowViewModel ();
			viewModel.PropertyChanged += ViewModelPropertyChanged;

			Build ();
			PopulateMessagingComboBox ();

			customMessageTextEntry.Text = viewModel.CustomText;

			clearLoggingTextButton.Clicked += ClearLoggingTextButtonClicked;
			sendLogMessageButton.Clicked += SendLogMessageButtonClicked;
			showMessageButton.Clicked += ShowMessageButtonClicked;
			showMessageRequestButton.Clicked += ShowMessageRequestButtonClicked;
			sendDiagnosticsButton.Clicked += SendDiagnosticsButtonClicked;
			addDiagnosticButton.Clicked += AddDiagnosticButtonClicked;
			clearDiagnosticsButton.Clicked += ClearDiagnosticsButtonClicked;
			viewModel.Tags.CollectionChanged += TagsCollectionChanged;
			customMessageTextEntry.Changed += CustomMessageTextEntryChanged;

			OnDiagnosticTagsChanged ();
		}

		protected override void OnClosed ()
		{
			base.OnClosed ();
			Application.Exit ();
		}

		public void Log (string message)
		{
			Application.Invoke (() => {
				AppendLogMessage (message);
			});
		}

		void AppendLogMessage (string message)
		{
			string text = loggingTextView.PlainText;
			text += message + "\n";
			loggingTextView.LoadText (text, TextFormat.Plain);
		}

		void ClearLoggingTextButtonClicked (object sender, EventArgs e)
		{
			loggingTextView.LoadText (string.Empty, TextFormat.Plain);
		}

		void PopulateMessagingComboBox ()
		{
			foreach (MessageType messageType in Enum.GetValues (typeof (MessageType))) {
				messagingComboBox.Items.Add (messageType);
			}

			messagingComboBox.SelectedIndex = 0;
		}

		void SendLogMessageButtonClicked (object sender, EventArgs e)
		{
			viewModel.LogMessage = messagingTextEntry.Text;
			viewModel.MessageType = (MessageType)messagingComboBox.SelectedItem;
			viewModel.SendLogMessage ();
		}

		void ShowMessageButtonClicked (object sender, EventArgs e)
		{
			viewModel.LogMessage = messagingTextEntry.Text;
			viewModel.MessageType = (MessageType)messagingComboBox.SelectedItem;
			viewModel.SendMessage ();
		}

		void ViewModelPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (viewModel.ResponseText)) {
				responseTextEntry.Text = viewModel.ResponseText;
			} else if (e.PropertyName == nameof (viewModel.CurrentSettings)) {
				settingsTextView.LoadText (viewModel.CurrentSettings ?? string.Empty, TextFormat.Plain);
			}
		}

		void ShowMessageRequestButtonClicked (object sender, EventArgs e)
		{
			viewModel.LogMessage = messagingTextEntry.Text;
			viewModel.MessageType = (MessageType)messagingComboBox.SelectedItem;
			viewModel.SendMessageRequest ();
		}

		void SendDiagnosticsButtonClicked (object sender, EventArgs e)
		{
			viewModel.SendDiagnostics ();
		}

		void ClearDiagnosticsButtonClicked (object sender, EventArgs e)
		{
			viewModel.Tags.Clear ();
		}

		void AddDiagnosticButtonClicked (object sender, EventArgs e)
		{
			viewModel.Tags.Add (new DiagnosticTag ());
		}

		void TagsCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			OnDiagnosticTagsChanged ();
		}

		void OnDiagnosticTagsChanged ()
		{
			foreach (DiagnosticWidget widget in diagnosticWidgets) {
				diagnosticWidgetsVBox.Remove (widget);
				widget.Dispose ();
			}

			diagnosticWidgets.Clear ();

			foreach (DiagnosticTag diagnostic in viewModel.Tags) {
				var widget = new DiagnosticWidget (diagnostic);
				diagnosticWidgets.Add (widget);
				diagnosticWidgetsVBox.PackStart (widget);
			}
		}

		void CustomMessageTextEntryChanged (object sender, EventArgs e)
		{
			viewModel.CustomText = customMessageTextEntry.Text;
		}
	}
}
