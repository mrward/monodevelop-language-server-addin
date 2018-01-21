//
// TextEditorExtensions.cs
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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.FindInFiles;

namespace MonoDevelop.LanguageServer.Client
{
	static class TextEditorExtensions
	{
		public static int PositionToOffset (this TextEditor editor, Position position)
		{
			Runtime.AssertMainThread ();

			return editor.LocationToOffset (position.Line + 1, position.Character + 1);
		}

		public static void StartTextEditorRename (this TextEditor editor, IEnumerable<SearchResult> references)
		{
			var oldVersion = editor.Version;

			var links = editor.CreateTextLinks (references);

			editor.StartTextLinkMode (new TextLinkModeOptions (links, (arg) => {
				if (!arg.Success) {
					List<TextChangeEventArgs> eventArgs = editor.Version.GetChangesTo (oldVersion).ToList ();
					foreach (TextChangeEventArgs eventArg in eventArgs) {
						foreach (TextChange textChange in eventArg.TextChanges) {
							editor.ReplaceText (textChange.Offset, textChange.RemovalLength, textChange.InsertedText);
						}
					}
				}
			}));
		}

		static List<TextLink> CreateTextLinks (this TextEditor editor, IEnumerable<SearchResult> references)
		{
			var links = new List<TextLink> ();
			var link = new TextLink ("name");

			foreach (SearchResult reference in references) {
				var segment = new TextSegment (reference.Offset, reference.Length);
				if (segment.Offset <= editor.CaretOffset && editor.CaretOffset <= segment.EndOffset) {
					link.Links.Insert (0, segment);
				} else {
					link.AddLink (segment);
				}
			}
			links.Add (link);

			return links;
		}

		public static TextSegment GetTextSegment (this TextEditor editor, Range range)
		{
			if (range != null) {
				int startOffset = editor.PositionToOffset (range.Start);
				int endOffset = editor.PositionToOffset (range.End);
				return new TextSegment (startOffset, endOffset - startOffset);
			}

			return TextSegment.Invalid;
		}

		public static SearchResult CreateSearchResult (this TextEditor editor, Location location)
		{
			int startOffset = editor.PositionToOffset (location.Range.Start);
			int endOffset = editor.PositionToOffset (location.Range.End);
			var provider = new FileProvider (new FilePath (location.Uri), null, startOffset, endOffset);
			return new SearchResult (provider, startOffset, endOffset - startOffset);
		}

		public static void ApplyEdits (this TextEditor editor, IEnumerable<TextEdit> edits)
		{
			Runtime.AssertMainThread ();

			if (edits == null || !edits.Any ()) {
				return;
			}

			var changes = edits
				.Select (edit => ToCodeAnalysisTextChange (editor, edit))
				.ToArray ();

			editor.ApplyTextChanges (changes);
		}

		static Microsoft.CodeAnalysis.Text.TextChange ToCodeAnalysisTextChange (TextEditor editor, TextEdit edit)
		{
			var segment = editor.GetTextSegment (edit.Range);

			if (segment.IsInvalid) {
				throw new ArgumentException (string.Format ("Invalid TextEdit.Range."));
			}

			return new Microsoft.CodeAnalysis.Text.TextChange (
				new Microsoft.CodeAnalysis.Text.TextSpan (segment.Offset, segment.Length),
				edit.NewText
			);
		}
	}
}
