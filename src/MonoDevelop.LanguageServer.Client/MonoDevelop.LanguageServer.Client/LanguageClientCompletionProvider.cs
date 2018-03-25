﻿//
// LanguageClientCompletionProvider.cs
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientCompletionProvider
	{
		JsonRpc jsonRpc;
		ILanguageClientCompletionProvider completionProvider;

		public LanguageClientCompletionProvider (JsonRpc jsonRpc, ILanguageClientCompletionProvider completionProvider)
		{
			this.jsonRpc = jsonRpc;
			this.completionProvider = completionProvider;
		}

		public Task<object> RequestCompletions (TextDocumentPositionParams positionParams, CancellationToken token)
		{
			if (completionProvider != null) {
				return completionProvider.RequestCompletions (positionParams, param => RequestCompletionsInternal (param, token));
			}
			return RequestCompletionsInternal (positionParams, token);
		}

		Task<object> RequestCompletionsInternal (TextDocumentPositionParams param, CancellationToken token)
		{
			var completionParams = new CompletionParams {
				Position = param.Position,
				TextDocument = param.TextDocument
			};
			return jsonRpc.InvokeWithParameterObjectAsync (Methods.TextDocumentCompletion, completionParams, token);
		}

		public Task<CompletionItem> ResolveCompletion (CompletionItem completionItem, CancellationToken token)
		{
			if (completionProvider != null) {
				return completionProvider.ResolveCompletion (completionItem, item => ResolveCompletionInternal (item, token));
			}
			return ResolveCompletionInternal (completionItem, token);
		}

		Task<CompletionItem> ResolveCompletionInternal (CompletionItem completionItem, CancellationToken token)
		{
			return jsonRpc.InvokeWithParameterObjectAsync (
				Methods.TextDocumentCompletionResolve,
				completionItem,
				token);
		}
	}
}
