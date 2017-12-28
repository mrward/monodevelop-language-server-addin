//
// IReadonlyTextDocumentExtensionsTests.cs
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

using System.Threading.Tasks;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Util;
using NUnit.Framework;

namespace MonoDevelop.LanguageServer.Client.Tests
{
	[TestFixture]
	public class IReadonlyTextDocumentExtensionsTests
	{
		IReadonlyTextDocument document;

		public async Task CreateTextDocument (string text)
		{
			var textSource = new StringTextSource (text);
			document = await SimpleReadonlyDocument.CreateReadonlyDocumentAsync (textSource);
		}

		[TestCase ("a", 1, 1, "a", 0, 1)]
		[TestCase ("a", 1, 2, "a", 0, 1)]
		[TestCase ("a b", 1, 1, "a", 0, 1)]
		[TestCase ("a b", 1, 2, "a", 0, 1)]
		[TestCase ("a b", 1, 3, "b", 2, 3)]
		[TestCase ("a b", 1, 4, "b", 2, 3)]
		[TestCase ("", 1, 1, null, -1, -1)]
		[TestCase ("a", 2, 1, null, -1, -1)]
		[TestCase ("a", 1, 3, null, -1, -1)]
		[TestCase ("a", 0, 3, null, -1, -1)]
		[TestCase ("a", 1, 0, null, -1, -1)]
		[TestCase ("ab", 1, 1, "ab", 0, 2)]
		[TestCase ("ab", 1, 2, "ab", 0, 2)]
		[TestCase ("ab cd", 1, 1, "ab", 0, 2)]
		[TestCase ("ab cd", 1, 2, "ab", 0, 2)]
		[TestCase ("ab cd", 1, 4, "cd", 3, 5)]
		[TestCase ("ab cd", 1, 5, "cd", 3, 5)]
		[TestCase ("a[b]", 1, 1, "a", 0, 1)]
		[TestCase ("a[b]", 1, 2, "a", 0, 1)]
		[TestCase ("a[b]", 1, 3, "b", 2, 3)]
		[TestCase ("a[b]", 1, 4, "b", 2, 3)]
		[TestCase ("a[b]", 1, 5, null, -1, -1)]
		public async Task GetWordAtPosition (
			string text,
			int line,
			int column,
			string expectedText,
			int expectedColumn,
			int expectedEndColumn)
		{
			await CreateTextDocument (text);

			var word = document.GetWordAtPosition (line, column);

			int expectedLength = (expectedText ?? string.Empty).Length;

			Assert.AreEqual (expectedText, word.Text);
			Assert.AreEqual (expectedColumn, word.StartColumn);
			Assert.AreEqual (expectedLength, word.Length);
			Assert.AreEqual (expectedEndColumn, word.EndColumn);
		}
	}
}
