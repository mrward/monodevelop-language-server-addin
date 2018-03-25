//
// ITextDocumentExtensions.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.FindInFiles;

namespace MonoDevelop.LanguageServer.Client
{
	static class ITextDocumentExtensions
	{
		public static int PositionToOffset (this ITextDocument editor, Position position)
		{
			Runtime.AssertMainThread ();

			return editor.LocationToOffset (position.Line + 1, position.Character + 1);
		}

		public static TextSegment GetTextSegment (this ITextDocument editor, Range range)
		{
			if (range != null) {
				int startOffset = editor.PositionToOffset (range.Start);
				int endOffset = editor.PositionToOffset (range.End);
				return new TextSegment (startOffset, endOffset - startOffset);
			}

			return TextSegment.Invalid;
		}

		public static void ApplyEdits (this ITextDocument editor, IEnumerable<TextEdit> edits)
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

		static Microsoft.CodeAnalysis.Text.TextChange ToCodeAnalysisTextChange (ITextDocument editor, TextEdit edit)
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

		public static SearchResult CreateSearchResult (this ITextDocument document, Location location)
		{
			int startOffset = document.PositionToOffset (location.Range.Start);
			int endOffset = document.PositionToOffset (location.Range.End);
			var provider = new FileProvider (location.Uri.ToFilePath (), null, startOffset, endOffset);
			return new SearchResult (provider, startOffset, endOffset - startOffset);
		}
	}
}
