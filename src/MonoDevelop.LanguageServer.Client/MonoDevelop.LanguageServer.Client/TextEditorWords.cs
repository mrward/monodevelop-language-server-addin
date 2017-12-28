//
// TextEditorWords.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Based on:
// https://github.com/Microsoft/vscode/src/vs/editor/common/model/wordHelper.ts
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

using System.Text.RegularExpressions;
using System;

namespace MonoDevelop.LanguageServer.Client
{
	static class TextEditorWords
	{
		public const string UsualWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:\'\",.<>/?";

		public static Regex DefaultWordRegex = CreateWordRegExp ();

		static Regex CreateWordRegExp (string allowInWords = "")
		{
			string usualSeparators = UsualWordSeparators;
			string source = "(-?\\d*\\.\\d\\w*)|([^";

			for (var i = 0; i < usualSeparators.Length; i++) {
				if (allowInWords.IndexOf (usualSeparators [i]) >= 0) {
					continue;
				}
				source += "\\" + usualSeparators[i];
			}
			source += "\\s]+)";

			return new Regex (source);
		}

		/// <summary>
		/// Does not handle word definition patterns that include spaces.
		/// Columns start from 1.
		/// </summary>
		public static WordAtPosition GetWordAtPosition (int column, Regex wordDefinition, string text)
		{
			MatchCollection matches = wordDefinition.Matches (text);

			int position = column - 1; // Columns start from 1.

			foreach (Match match in matches) {
				string word = match.Value;
				int endWord = match.Index + match.Length;
				int startWord = match.Index;

				int startColumn = startWord;
				int endColumn = endWord;

				if (startColumn <= position && position <= endColumn) {
					// Console.WriteLine ("GetWordAtPosition: '{0}' start={1},length={2}", word, startColumn, word.Length);
					return new WordAtPosition (word, startColumn);
				}
			}

			return WordAtPosition.Invalid;
		}
	}
}
