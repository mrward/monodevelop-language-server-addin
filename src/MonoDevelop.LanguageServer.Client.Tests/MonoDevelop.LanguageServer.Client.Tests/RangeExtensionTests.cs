//
// RangeExtensionTests.cs
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

using Microsoft.VisualStudio.LanguageServer.Protocol;
using NUnit.Framework;

namespace MonoDevelop.LanguageServer.Client.Tests
{
	[TestFixture]
	public class RangeExtensionTests
	{
		static Range CreateRange (int line, int col, int endLine, int endCol)
		{
			return new Range {
				Start = new Position (line, col),
				End = new Position (endLine, endCol)
			};
		}

		[Test]
		public void ContainsPositionTests ()
		{
			Range range = CreateRange (line: 2, col: 0, endLine: 2, endCol: 1);

			Assert.IsTrue (range.Contains (new Position (2, 0)));
			Assert.IsTrue (range.Contains (new Position (2, 1)));
			Assert.IsFalse (range.Contains (new Position (1, 0)));
			Assert.IsFalse (range.Contains (new Position (1, 1)));
			Assert.IsFalse (range.Contains (new Position (3, 0)));
			Assert.IsFalse (range.Contains (new Position (3, 1)));
		}

		[Test]
		public void IsOverlappingTests ()
		{
			Range range1 = CreateRange (line: 2, col: 0, endLine: 2, endCol: 1);
			Range range2 = CreateRange (line: 2, col: 0, endLine: 2, endCol: 1);

			Assert.IsTrue (range1.IsOverlapping (range2));
			Assert.IsTrue (range2.IsOverlapping (range1));

			range1 = CreateRange (line: 2, col: 0, endLine: 2, endCol: 1);
			range2 = CreateRange (line: 2, col: 1, endLine: 2, endCol: 1);

			Assert.IsTrue (range1.IsOverlapping (range2));
			Assert.IsTrue (range2.IsOverlapping (range1));

			range1 = CreateRange (line: 2, col: 0, endLine: 2, endCol: 1);
			range2 = CreateRange (line: 2, col: 0, endLine: 2, endCol: 0);

			Assert.IsTrue (range1.IsOverlapping (range2));
			Assert.IsTrue (range2.IsOverlapping (range1));

			range1 = CreateRange (line: 2, col: 0, endLine: 2, endCol: 1);
			range2 = CreateRange (line: 2, col: 2, endLine: 2, endCol: 3);

			Assert.IsFalse (range1.IsOverlapping (range2));
			Assert.IsFalse (range2.IsOverlapping (range1));

			range1 = CreateRange (line: 2, col: 0, endLine: 4, endCol: 0);
			range2 = CreateRange (line: 3, col: 0, endLine: 3, endCol: 0);

			Assert.IsTrue (range1.IsOverlapping (range2));
			Assert.IsTrue (range2.IsOverlapping (range1));

			range1 = CreateRange (line: 2, col: 0, endLine: 2, endCol: 1);
			range2 = CreateRange (line: 1, col: 0, endLine: 1, endCol: 10);

			Assert.IsFalse (range1.IsOverlapping (range2));
			Assert.IsFalse (range2.IsOverlapping (range1));
		}
	}
}
