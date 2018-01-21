//
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientCompletionData : CompletionData
	{
		LanguageClientSession session;
		bool resolved;

		public LanguageClientCompletionData (LanguageClientSession session, CompletionItem item)
		{
			this.session = session;
			CompletionItem = item;
			CompletionText = item.InsertText ?? item?.TextEdit?.NewText;

			if (CompletionText == null) {
				CompletionText = item.Label;
			}

			Icon = item.GetIcon ();
		}

		public CompletionItem CompletionItem { get; private set; }

		public override string DisplayText {
			get { return CompletionItem.Label ?? CompletionText; }
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
			string description = CompletionItem.Detail;

			if (CompletionItem.Documentation != null) {
				if (description == null) {
					description = CompletionItem.Documentation;
				} else {
					description += Environment.NewLine + CompletionItem.Documentation;
				}
			}

			return description ?? string.Empty;
		}

		public override Task<TooltipInformation> CreateTooltipInformation (
			bool smartWrap,
			CancellationToken cancelToken)
		{
			if (!session.IsCompletionResolveProvider || resolved) {
				return base.CreateTooltipInformation (smartWrap, cancelToken);
			}

			try {
				return CreateTooltipWithResolvedCompletionItem (smartWrap, cancelToken);
			} catch (OperationCanceledException) {
				// Ignore.
				return base.CreateTooltipInformation (smartWrap, cancelToken);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("Unable to resolve completion item.", ex);
				return base.CreateTooltipInformation (smartWrap, cancelToken);
			}
		}

		async Task<TooltipInformation> CreateTooltipWithResolvedCompletionItem (bool smartWrap, CancellationToken token)
		{
			var resolvedCompletionItem = await session.ResolveCompletionItem (CompletionItem, token);
			if (resolvedCompletionItem != null) {
				CompletionItem = resolvedCompletionItem;
				resolved = true;
			}

			return await base.CreateTooltipInformation (smartWrap, token);
		}
	}
}
