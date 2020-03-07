// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Based on:
// https://github.com/dotnet/roslyn/blob/e704ca635bd6de70a0250e34c4567c7a28fa9f6d/src/Features/Core/Portable/Completion/CommonCompletionUtilities.cs

using System;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Completion
{
	static class CommonCompletionUtilities
	{
		public static TextSpan GetWordSpan (ITextView textView, int position,
			Func<char, bool> isWordStartCharacter, Func<char, bool> isWordCharacter)
		{
			return GetWordSpan (textView, position, isWordStartCharacter, isWordCharacter, alwaysExtendEndSpan: false);
		}

		public static TextSpan GetWordSpan (ITextView textView, int position,
			Func<char, bool> isWordStartCharacter, Func<char, bool> isWordCharacter, bool alwaysExtendEndSpan = false)
		{
			var text = textView.TextSnapshot;
			var start = position;
			while (start > 0 && isWordStartCharacter (text[start - 1])) {
				start--;
			}

			// If we're brought up in the middle of a word, extend to the end of the word as well.
			// This means that if a user brings up the completion list at the start of the word they
			// will "insert" the text before what's already there (useful for qualifying existing
			// text).  However, if they bring up completion in the "middle" of a word, then they will
			// "overwrite" the text. Useful for correcting misspellings or just replacing unwanted
			// code with new code.
			var end = position;
			if (start != position || alwaysExtendEndSpan) {
				while (end < text.Length && isWordCharacter (text[end])) {
					end++;
				}
			}

			return TextSpan.FromBounds (start, end);
		}

		public static TextSpan GetDefaultCompletionListSpan (ITextView textView, int caretPosition)
		{
			return GetWordSpan (
				textView,
				caretPosition,
				c => char.IsLetter (c),
				c => char.IsLetterOrDigit (c));
		}
	}
}
