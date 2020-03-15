//
// LanguageClientAsyncCompletionProvider.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2020 Microsoft Corporation
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

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

using CommonCompletionUtilities = Microsoft.CodeAnalysis.Completion.CommonCompletionUtilities;
using ProtocolCompletionItem = Microsoft.VisualStudio.LanguageServer.Protocol.CompletionItem;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientAsyncCompletionSource : IAsyncCompletionSource
	{
		internal const string LanguageClientCompletionItem = nameof (LanguageClientCompletionItem);

		readonly LanguageClientSession session;
		readonly ITextView textView;

		public LanguageClientAsyncCompletionSource (LanguageClientSession session, ITextView textView)
		{
			this.session = session;
			this.textView = textView;
		}

		public async Task<CompletionContext> GetCompletionContextAsync (
			IAsyncCompletionSession completionSession,
			CompletionTrigger trigger,
			SnapshotPoint triggerLocation,
			SnapshotSpan applicableToSpan,
			CancellationToken token)
		{
			string fileName = textView.GetFileName();

			(int line, int column) = triggerLocation.GetLineAndColumn1Based ();

			var list = await session.GetCompletionItems (fileName, line, column, token);

			if (list?.Items == null) {
				return new CompletionContext (ImmutableArray<CompletionItem>.Empty);
			}

			var completionItems = ImmutableArray.CreateBuilder<CompletionItem> ();
			foreach (var item in list.Items) {
				var completionItem = new CompletionItem (
					item.Label,
					this,
					item.GetImageElement (),
					ImmutableArray<CompletionFilter>.Empty,
					string.Empty,
					item.GetInsertText (),
					item.SortText ?? item.Label,
					item.FilterText ?? item.Label,
					ImmutableArray<ImageElement>.Empty);

				completionItem.Properties [LanguageClientCompletionItem] = item;

				completionItems.Add (completionItem);
			}
			return new CompletionContext (completionItems.ToImmutableArray ());
		}

		public Task<object> GetDescriptionAsync (
			IAsyncCompletionSession completionSession,
			CompletionItem item,
			CancellationToken token)
		{
			if (item.Properties.TryGetProperty (LanguageClientCompletionItem, out ProtocolCompletionItem completionItem)) {
				return Task.FromResult<object> (completionItem.Detail);
			}
			return Task.FromResult<object> (null);
		}

		public CompletionStartData InitializeCompletion (
			CompletionTrigger trigger,
			SnapshotPoint triggerLocation,
			CancellationToken token)
		{
			if (textView.Selection.Mode == TextSelectionMode.Box) {
				// Multiple selection - not supported.
				return new CompletionStartData (CompletionParticipation.DoesNotProvideItems);
			}

			if (!session.IsCompletionProvider) {
				return new CompletionStartData (CompletionParticipation.DoesNotProvideItems);
			}

			if (!ShouldTriggerCompletion (trigger, triggerLocation)) {
				return new CompletionStartData (CompletionParticipation.DoesNotProvideItems);
			}

			SnapshotSpan applicableToSpan = GetApplicableToSpan (triggerLocation);
			return new CompletionStartData (CompletionParticipation.ProvidesItems, applicableToSpan);
		}

		bool ShouldTriggerCompletion (CompletionTrigger trigger, SnapshotPoint triggerLocation)
		{
			if (trigger.Reason == CompletionTriggerReason.Insertion) {
				if (session.IsCompletionTriggerCharacter (trigger.Character)) {
					return true;
				} else if (trigger.Character == '\n') {
					return false;
				} else if (trigger.Character == '\t') {
					// No snippet support currently.
					return false;
				} else if (session.IsSignatureHelpTriggerCharacter(trigger.Character)) {
					return false;
				}
				// TODO - restrict?
				return true;
			} else if (trigger.Reason == CompletionTriggerReason.Invoke ||
				trigger.Reason == CompletionTriggerReason.InvokeAndCommitIfUnique) {
				return true;
			}

			return false;
		}

		SnapshotSpan GetApplicableToSpan (SnapshotPoint triggerLocation)
		{
			var textSpan = CommonCompletionUtilities.GetDefaultCompletionListSpan (textView, triggerLocation.Position);
			return new SnapshotSpan (
				triggerLocation.Snapshot,
				new Span (textSpan.Start, textSpan.Length));
		}
	}
}