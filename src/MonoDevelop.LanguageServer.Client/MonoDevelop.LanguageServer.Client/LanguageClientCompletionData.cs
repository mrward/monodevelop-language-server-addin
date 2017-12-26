﻿//
// LanguageClientCompletionData.cs
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

using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientCompletionData : CompletionData
	{
		LanguageClientSession session;

		public LanguageClientCompletionData (LanguageClientSession session, CompletionItem item)
		{
			this.session = session;
			CompletionItem = item;
			CompletionText = item.InsertText;

			Icon = item.GetIcon ();
		}

		public CompletionItem CompletionItem { get; private set; }

		/// <summary>
		/// Returns InsertText instead of Label so parameters are handled (e.g. -path).
		/// VSCode shows only the parameter name without the '-'. It is simpler to
		/// show the '-' as part of the text in the code completion list in MonoDevelop
		/// than to support it as VSCode does.
		/// </summary>
		public override string DisplayText {
			get { return CompletionItem.InsertText; }
			set { base.DisplayText = value; }
		}

		public override string Description {
			get {
				return GLib.Markup.EscapeText (GetDescription ());
			}
			set { base.Description = value; }
		}

		string GetDescription ()
		{
			return CompletionItem.Detail ?? string.Empty;
		}
	}
}