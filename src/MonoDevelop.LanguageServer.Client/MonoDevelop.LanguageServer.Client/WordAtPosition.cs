//
// WordAtPosition.cs
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

namespace MonoDevelop.LanguageServer.Client
{
	struct WordAtPosition : IEquatable<WordAtPosition>
	{
		public static readonly WordAtPosition Invalid = new WordAtPosition (null, -1);

		public WordAtPosition (string text, int startColumn)
		{
			Text = text;

			if (text != null) {
				Length = text.Length;
			} else {
				Length = 0;
			}

			StartColumn = startColumn;
			EndColumn = startColumn + Length;
		}

		public string Text { get; private set; }
		public int StartColumn { get; private set; }
		public int EndColumn { get; private set; }
		public int Length { get; private set; }

		public bool IsEmpty {
			get { return EndColumn == StartColumn; }
		}

		public bool IsInvalid {
			get {
				return (StartColumn < 0) || (EndColumn < 0);
			}
		}

		public override bool Equals (object obj)
		{
			if (obj is WordAtPosition) {
				return Equals ((WordAtPosition)obj);
			}

			return false;
		}

		public bool Equals (WordAtPosition wordAtPosition)
		{
			return (StartColumn == wordAtPosition.StartColumn) &&
				(EndColumn == wordAtPosition.EndColumn) &&
				(Text == wordAtPosition.Text);
		}

		public override int GetHashCode ()
		{
			return Text.GetHashCode ();
		}

		public override string ToString ()
		{
			return string.Format (
				"Text='{0}', StartColumn={1}, EndColumn={2}, Length={3}",
				Text,
				StartColumn,
				EndColumn,
				Length);
		}
	}
}
