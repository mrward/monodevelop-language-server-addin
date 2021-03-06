﻿//
// TextChangeEventArgsExtensions.cs
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
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.LanguageServer.Client
{
	[Obsolete]
	static class TextChangeEventArgsExtensions
	{
		public static IEnumerable<TextDocumentContentChangeEvent> CreateTextDocumentContentChangeEvents (
			this TextChangeEventArgs e,
			TextEditor editor,
			bool fullContent)
		{
			if (fullContent) {
				return CreateFullTextDocumentContentChangeEvents (editor);
			}

			return e.TextChanges.Select (textChange => {
				return CreateIncrementalTextDocumentContentChangeEvent (textChange, editor);
			});
		}

		static TextDocumentContentChangeEvent CreateIncrementalTextDocumentContentChangeEvent (
			this TextChange textChange,
			TextEditor editor)
		{
			int startOffset = textChange.NewOffset;
			int endOffset = startOffset + textChange.RemovalLength;

			var startLocation = editor.OffsetToLocation (startOffset);

			DocumentLocation endLocation;

			if (textChange.RemovalLength == 0) {
				endLocation = editor.OffsetToLocation (endOffset);
			} else {
				endLocation = GetEndLocationAfterRemoval (startLocation, textChange);
			}

			return new TextDocumentContentChangeEvent {
				Range = new Range {
					Start = startLocation.CreatePosition (),
					End = endLocation.CreatePosition ()
				},
				RangeLength = endOffset - startOffset,
				Text = textChange.InsertedText.Text
			};
		}

		static IEnumerable<TextDocumentContentChangeEvent> CreateFullTextDocumentContentChangeEvents (
			TextEditor editor)
		{
			yield return new TextDocumentContentChangeEvent {
				Text = editor.Text
			};
		}

		static DocumentLocation GetEndLocationAfterRemoval (DocumentLocation startLocation, TextChange textChange)
		{
			var document = TextEditorFactory.CreateNewReadonlyDocument (textChange.RemovedText, "a.txt");
			if (document.LineCount > 1) {
				int line = startLocation.Line + document.LineCount - 1;

				IDocumentLine lastLine = document.GetLine (document.LineCount);
				int column = lastLine.LengthIncludingDelimiter + 1;

				return new DocumentLocation (line, column);
			} else {
				int column = startLocation.Column + textChange.RemovalLength;
				return new DocumentLocation (startLocation.Line, column);
			}
		}
	}
}
