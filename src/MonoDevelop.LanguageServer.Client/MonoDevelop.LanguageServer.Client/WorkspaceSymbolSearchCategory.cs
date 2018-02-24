//
// WorkspaceSymbolSearchCategory.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.LanguageServer.Client
{
	class WorkspaceSymbolSearchCategory : SearchCategory
	{
		static readonly string [] tags = { "symbol" };

		public WorkspaceSymbolSearchCategory ()
			: base (GettextCatalog.GetString ("Symbol"))
		{
			sortOrder = FirstCategory;
		}

		public override string[] Tags {
			get { return tags; }
		}

		public override bool IsValidTag (string tag)
		{
			return tags.Contains (tag);
		}

		public override Task GetResults (
			ISearchResultCallback searchResultCallback,
			SearchPopupSearchPattern pattern,
			CancellationToken token)
		{
			var activeDocument = IdeApp.Workbench.ActiveDocument;
			if (activeDocument != null) {
				return GetResults (activeDocument, searchResultCallback, pattern, token);
			}
			return Task.CompletedTask;
		}

		Task GetResults (
			Document activeDocument,
			ISearchResultCallback searchResultCallback,
			SearchPopupSearchPattern pattern,
			CancellationToken token)
		{
			LanguageClientSession session = LanguageClientServices.Workspace.GetSession (activeDocument, false);
			if (session?.IsWorkspaceSymbolProvider == true) {
				return GetResults (session, searchResultCallback, pattern, token);
			}

			return Task.CompletedTask;
		}

		async Task GetResults (
			LanguageClientSession session,
			ISearchResultCallback searchResultCallback,
			SearchPopupSearchPattern pattern,
			CancellationToken token)
		{
			SymbolInformation[] results = await session.GetWorkspaceSymbols (pattern.Pattern, token).ConfigureAwait (false);
			if (results != null && results.Length > 0) {
				foreach (var result in results) {
					var searchResult = new WorkspaceSymbolSearchResult (pattern.Pattern, result);
					searchResultCallback.ReportResult (searchResult);
				}
			}
		}
	}
}
