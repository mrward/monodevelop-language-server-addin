//
// DiagnosticWidget.cs
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
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace LanguageServer.UI
{
	partial class DiagnosticWidget
	{
		DiagnosticTag diagnostic;

		public DiagnosticWidget (DiagnosticTag diagnostic)
		{
			this.diagnostic = diagnostic;

			Build ();

			diagnosticTextEntry.Text = diagnostic.Text;
			PopulateDiagnosticSeverityComboBox ();

			diagnosticTextEntry.Changed += DiagnosticTextEntryChanged;
			diagnosticSeverityComboBox.SelectionChanged += DiagnosticSeverityComboBoxSelectionChanged;
		}

		public DiagnosticTag Diagnostic {
			get { return diagnostic; }
		}

		void PopulateDiagnosticSeverityComboBox ()
		{
			foreach (DiagnosticSeverity severity in Enum.GetValues (typeof (DiagnosticSeverity))) {
				diagnosticSeverityComboBox.Items.Add (severity);
			}

			if (diagnostic.Severity == 0) {
				diagnostic.Severity = DiagnosticSeverity.Error;
			}

			diagnosticSeverityComboBox.SelectedItem = diagnostic.Severity;
		}

		void DiagnosticTextEntryChanged (object sender, EventArgs e)
		{
			diagnostic.Text = diagnosticTextEntry.Text;
		}

		void DiagnosticSeverityComboBoxSelectionChanged (object sender, EventArgs e)
		{
			diagnostic.Severity = (DiagnosticSeverity)diagnosticSeverityComboBox.SelectedItem;
		}
	}
}
