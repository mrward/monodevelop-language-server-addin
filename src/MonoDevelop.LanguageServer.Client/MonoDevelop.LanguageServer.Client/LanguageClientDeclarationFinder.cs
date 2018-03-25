//
// LanguageClientDeclarationFinder.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientDeclarationFinder
	{
		TextEditor editor;
		LanguageClientSession session;

		public LanguageClientDeclarationFinder (TextEditor editor, LanguageClientSession session)
		{
			this.editor = editor;
			this.session = session;
		}

		public async Task OpenDeclaration (FilePath fileName, DocumentLocation location)
		{
			ProgressMonitor monitor = null;

			try {
				using (monitor = LanguageClientProgressMonitors.GetOpenDeclarationProgressMonitor ()) {
					Location[] locations = await session.FindDefinitions (
						fileName,
						location.CreatePosition (),
						monitor.CancellationToken);

					if (locations == null || locations.Length == 0) {
						monitor.ReportNoDeclarationFound ();
					} else if (locations.Length == 1) {
						OpenDeclaration (locations [0]);
					} else {
						ShowMultipleDeclarations (locations);
					}
				}
			} catch (OperationCanceledException) {
				LanguageClientLoggingService.Log ("Go to declaration canceled.");
				if (monitor != null) {
					monitor.ReportGoToDeclarationCanceled ();
				}
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("OpenDeclaration error.", ex);
			}
		}

		static void OpenDeclaration (Location location)
		{
			var fileInfo = new FileOpenInformation (location.Uri.ToFilePath ());

			if (location.Range?.Start != null) {
				fileInfo.Line = location.Range.Start.Line + 1;
				fileInfo.Column = location.Range.Start.Character + 1;
			}

			IdeApp.Workbench.OpenDocument (fileInfo).LogFault ();
		}

		void ShowMultipleDeclarations (Location[] locations)
		{
			using (var monitor = LanguageClientProgressMonitors.GetSearchProgressMonitor ()) {
				List<SearchResult> references = locations.Select (CreateSearchResult).ToList ();
				monitor.ReportResults (references);
			}
		}

		SearchResult CreateSearchResult (Location location)
		{
			return editor.CreateSearchResult (location);
		}
	}
}
