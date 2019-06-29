//
// WorkspaceEditHandler.cs
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.LanguageServer.Client
{
	[Obsolete]
	class WorkspaceEditHandler
	{
		public static void ApplyChanges (WorkspaceEdit edit)
		{
			foreach (KeyValuePair<string, TextEdit[]> item in edit.Changes) {
				ApplyChanges (item.Key, item.Value);
			}
		}

		static void ApplyChanges (string fileName, IEnumerable<TextEdit> edits)
		{
			bool open = false;
			ITextDocument document = TextFileProvider.Instance.GetTextEditorData (fileName, out open);
			if (document != null) {
				document.ApplyEdits (edits);
				if (!open) {
					document.Save ();
				}
			} else {
				LanguageClientLoggingService.Log ("Unable to find text editor for file: '{0}'", fileName);
			}
		}

		public static void ApplyChanges (IEnumerable<Location> locations, string newText)
		{
			foreach (IGrouping<string, Location> groupedByFileName in locations.GroupBy (location => location.Uri)) {
				var edits = groupedByFileName.Select (location => new TextEdit {
					NewText = newText,
					Range = location.Range
				});
				ApplyChanges (groupedByFileName.Key, edits);
			}
		}
	}
}
