//
// LanguageClientReferencesFinder.cs
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

namespace MonoDevelop.LanguageServer.Client
{
	[Obsolete]
	class LanguageClientReferencesFinder
	{
		TextEditor editor;
		LanguageClientSession session;

		public LanguageClientReferencesFinder (TextEditor editor, LanguageClientSession session)
		{
			this.editor = editor;
			this.session = session;
		}

		public async Task FindReferences (FilePath fileName, DocumentLocation location)
		{
			try {
				using (var monitor = LanguageClientProgressMonitors.GetSearchProgressMonitor ()) {
					Location[] locations = await session.GetReferences (
						fileName,
						location.CreatePosition (),
						monitor.CancellationToken);

					if (locations == null) {
						monitor.ReportResults (Enumerable.Empty<SearchResult> ());
					} else {
						List<SearchResult> references = ToSearchResults (locations).ToList ();
						monitor.ReportResults (references);
					}
				}
			} catch (OperationCanceledException) {
				LanguageClientLoggingService.Log ("Find references was canceled.");
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("FindReferences error.", ex);
			}
		}

		IEnumerable<SearchResult> ToSearchResults (IEnumerable<Location> locations)
		{
			return locations
				.Select (CreateSearchResult)
				.Where (result => result != null);
		}

		SearchResult CreateSearchResult (Location location)
		{
			ITextDocument document = TextFileProvider.Instance.GetTextEditorData (location.Uri.ToFilePath ());

			if (document != null) {
				return document.CreateSearchResult (location);
			}

			LoggingService.LogError ("Unable to find document for search result: '{0}'", location.Uri);

			return null;
		}

		public async Task RenameOccurrences (FilePath fileName, DocumentLocation location)
		{
			try {
				using (var monitor = LanguageClientProgressMonitors.GetSearchProgressMonitorForRename ()) {
					Location[] locations = await session.GetReferences (
						fileName,
						location.CreatePosition (),
						monitor.CancellationToken);

					if (locations == null) {
						monitor.ReportNoReferencesFound ();
						return;
					}

					List<SearchResult> references = ToSearchResults (locations).ToList ();
					if (!references.Any ()) {
						monitor.ReportNoReferencesFound ();
					} else if (AllSearchResultsExistInCurrentEditor (references)) {
						editor.StartTextEditorRename (references);
					} else {
						// Multiple files - cannot use text editor edit links.
						string oldName = GetSearchResultItemText (references [0]);
						string newName = RenameItemDialog.PromptForNewName (oldName);
						WorkspaceEditHandler.ApplyChanges (locations, newName);
					}
				}
			} catch (OperationCanceledException) {
				LanguageClientLoggingService.Log ("Rename was canceled.");
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("RenameOccurrences error.", ex);
			}
		}

		bool AllSearchResultsExistInCurrentEditor (IEnumerable<SearchResult> results)
		{
			return results.All (result => result.FileName == editor.FileName);
		}

		static string GetSearchResultItemText (SearchResult result)
		{
			ITextDocument document = TextFileProvider.Instance.GetTextEditorData (result.FileName);
			return document.GetTextAt (result.Offset, result.Length);
		}

		public async Task Rename (FilePath fileName, DocumentLocation location, string newName)
		{
			try {
				using (var monitor = LanguageClientProgressMonitors.GetSearchProgressMonitorForRename ()) {
					WorkspaceEdit edit = await session.Rename (
						fileName,
						location.CreatePosition (),
						newName,
						monitor.CancellationToken);

					if (edit?.Changes == null) {
						monitor.ReportNothingToRename ();
					} else {
						WorkspaceEditHandler.ApplyChanges (edit);
					}
				}
			} catch (OperationCanceledException) {
				LanguageClientLoggingService.Log ("Rename was canceled.");
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("Rename error.", ex);
			}
		}
	}
}
