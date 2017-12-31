//
// TextChangeEventArgsExtensionsTests.cs
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using NUnit.Framework;

namespace MonoDevelop.LanguageServer.Client.Tests
{
	[TestFixture]
	class TextChangeEventArgsExtensionsTests : TextEditorTestBase
	{
		TextEditor editor;
		TextChangeEventArgs textChangeEventArgs;

		int CreateTextEditor (string text)
		{
			int markerOffset = text.IndexOf ('|');
			if (markerOffset != -1) {
				text = text.Substring (0, markerOffset) + text.Substring (markerOffset + 1);
			}

			var textSource = new StringTextSource (text);
			editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = text;

			editor.TextChanged += DocumentTextChanged;

			return markerOffset;
		}

		void DocumentTextChanged (object sender, TextChangeEventArgs e)
		{
			textChangeEventArgs = e;
		}

		IEnumerable<TextDocumentContentChangeEvent> CreateTextChangeEvents (bool full)
		{
			return textChangeEventArgs.CreateTextDocumentContentChangeEvents (editor, full);
		}

		static void AssertPositionsAreEqual (string position1Text, Position position2)
		{
			string position2Text = PositionToString (position2);

			// Normalize position1 text.
			Position position1 = CreatePosition (position1Text);
			position1Text = PositionToString (position1);

			Assert.AreEqual (position1Text, position2Text);
		}

		static string PositionToString (Position position)
		{
			return string.Format ("{0},{1}", position.Line, position.Character);
		}

		/// <summary>
		/// The positionText is "line,col".
		/// </summary>
		static Position CreatePosition (string positionText)
		{
			int index = positionText.IndexOf (',');

			string lineText = positionText.Substring (0, index).Trim ();
			string columnText = positionText.Substring (index + 1).Trim ();

			int line = int.Parse (lineText);
			int column = int.Parse (columnText);

			return new Position (line, column);
		}

		[Test]
		public void TextEditorCreation ()
		{
			int markerOffset = CreateTextEditor ("a|b");

			Assert.AreEqual (1, markerOffset);
			Assert.AreEqual ("ab", editor.Text);
		}

		[TestCase ("a|c", "b", "abc", 3)]
		[TestCase ("|bc", "a", "abc", 3)]
		[TestCase ("ab|", "c", "abc", 3)]
		[TestCase ("a|c", "bb", "abbc", 4)]
		[TestCase ("a|c", "b\r\nb", "ab\r\nbc", 6)]
		public void InsertText_FullContentChangeEvent (
			string text,
			string insertText,
			string expectedTextChangeText,
			int expectedRangeLength)
		{
			int markerOffset = CreateTextEditor (text);

			editor.InsertText (markerOffset, insertText);

			var textChange = CreateTextChangeEvents (true).Single ();

			Assert.AreEqual (expectedTextChangeText, textChange.Text);
			Assert.AreEqual (expectedRangeLength, textChange.RangeLength);
		}

		[TestCase ("a|c", "b", "abc", "b", 0, "0,1", "0,1")]
		[TestCase ("|bc", "a", "abc", "a", 0, "0,0", "0,0")]
		[TestCase ("ab|", "c", "abc", "c", 0, "0,2", "0,2")]
		[TestCase ("ab|", "c\r\nd", "abc\r\nd", "c\r\nd", 0, "0,2", "0,2")]
		[TestCase ("ab\r\nc|d", "e", "ab\r\nced", "e", 0, "1,1", "1,1")]
		public void InsertText_IncrementalChangeEvent (
			string text,
			string insertText,
			string expectedText,
			string expectedTextChangeText,
			int expectedRangeLength,
			string expectedStartPosition,
			string expectedEndPosition)
		{
			int markerOffset = CreateTextEditor (text);

			editor.InsertText (markerOffset, insertText);

			var textChange = CreateTextChangeEvents (false).Single ();

			Assert.AreEqual (expectedText, editor.Text);
			Assert.AreEqual (expectedTextChangeText, textChange.Text);
			Assert.AreEqual (expectedRangeLength, textChange.RangeLength);
			AssertPositionsAreEqual (expectedStartPosition, textChange.Range.Start);
			AssertPositionsAreEqual (expectedEndPosition, textChange.Range.End);
		}

		[TestCase ("ac", "b", 1, 1, "ab", "b", 1, "0,1", "0,2")]
		[TestCase ("ac", "b", 0, 1, "bc", "b", 1, "0,0", "0,1")]
		[TestCase ("abc", "d", 0, 2, "dc", "d", 2, "0,0", "0,2")]
		[TestCase ("abc", "", 0, 2, "c", "", 2, "0,0", "0,2")]
		[TestCase ("ac", "", 0, 1, "c", "", 1, "0,0", "0,1")]
		[TestCase ("ac", "", 1, 1, "a", "", 1, "0,1", "0,2")]
		[TestCase ("abc\ndef", "e", 4, 1, "abc\neef", "e", 1, "1,0", "1,1")]
		[TestCase ("abc\ndef", "", 3, 1, "abcdef", "", 1, "0,3", "1,0")]
		[TestCase ("abc\n", "", 3, 1, "abc", "", 1, "0,3", "1,0")]
		[TestCase ("abc\r\n", "", 3, 2, "abc", "", 2, "0,3", "1,0")]
		public void ReplaceText_IncrementalChangeEvent (
			string text,
			string replaceText,
			int replaceOffset,
			int replaceCount,
			string expectedText,
			string expectedTextChangeText,
			int expectedRangeLength,
			string expectedStartPosition,
			string expectedEndPosition)
		{
			CreateTextEditor (text);

			editor.ReplaceText (replaceOffset, replaceCount, replaceText);

			var textChange = CreateTextChangeEvents (false).Single ();

			Assert.AreEqual (expectedText, editor.Text);
			Assert.AreEqual (expectedTextChangeText, textChange.Text);
			Assert.AreEqual (expectedRangeLength, textChange.RangeLength);
			AssertPositionsAreEqual (expectedStartPosition, textChange.Range.Start);
			AssertPositionsAreEqual (expectedEndPosition, textChange.Range.End);
		}
	}
}
