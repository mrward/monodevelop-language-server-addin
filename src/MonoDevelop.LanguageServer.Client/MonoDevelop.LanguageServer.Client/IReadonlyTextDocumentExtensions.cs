//
// IReadonlyTextDocumentExtensions.cs
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

using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.LanguageServer.Client
{
	static class IReadonlyTextDocumentExtensions
	{
		/// <summary>
		/// Gets the word at position. Line and column numbers start from 1.
		/// </summary>
		public static WordAtPosition GetWordAtPosition (this IReadonlyTextDocument document, int line, int column)
		{
			if (line <= 0 || column <= 0) {
				return WordAtPosition.Invalid;
			}

			if (line > 0 && (line > document.LineCount)) {
				return WordAtPosition.Invalid;
			}

			string lineText = document.GetLineText (line);

			// Check the column exists on the line. Allow the column to
			// be just after the last text character.
			if (column - 1 > lineText.Length) {
				return WordAtPosition.Invalid;
			}

			return TextEditorWords.GetWordAtPosition (
				column,
				TextEditorWords.DefaultWordRegex,
				lineText);
		}

		public static WordAtPosition GetWordAtPosition (
			this IReadonlyTextDocument document,
			CodeCompletionContext context)
		{
			return document.GetWordAtPosition (context.TriggerLine, context.TriggerLineOffset + 1);
		}
	}
}
