//
// LanguageClientSession.cs
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
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientSession : IDisposable
	{
		ILanguageClient client;
		JsonRpc jsonRpc;
		string fileExtension;
		CancellationToken cancellationToken = CancellationToken.None;

		public LanguageClientSession (ILanguageClient client, string fileExtension)
		{
			this.client = client;
			this.fileExtension = fileExtension;

			client.StartAsync += OnStartAsync;
		}

		public string Id {
			get { return fileExtension; }
		}

		public ServerCapabilities ServerCapabilities { get; private set; }
		public bool IsStarted { get; private set; }

		public event EventHandler Started;

		/// <summary>
		/// ILanguageClient.StartAsync += StartAsync;
		/// ILanguageClient.OnLoadedAsync ();
		/// StartAsync then calls ILanguageClient.ActivateAsync;
		/// </summary>
		public void Start ()
		{
			LanguageClientLoggingService.Log ("LanguageClient[{0}]: Call OnLoadedAsync.", Id);

			client.OnLoadedAsync ()
				.LogFault ();
		}

		public void Stop ()
		{
			if (client != null) {
				client.StartAsync -= OnStartAsync;
				client = null;
			}

			jsonRpc?.Dispose ();
			jsonRpc = null;
		}

		async Task OnStartAsync (object sender, EventArgs e)
		{
			try {
				await OnStartAsync ();
				IsStarted = true;
				Started?.Invoke (this, EventArgs.Empty);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError (ex, "LanguageClient[{0}]: OnStartAsync error.", Id);
			}
		}

		async Task OnStartAsync ()
		{
			LanguageClientLoggingService.Log ("LanguageClient[{0}]: Call ActivateAsync.", Id);

			Connection connection = await client.ActivateAsync (CancellationToken.None);

			LanguageClientLoggingService.Log ("LanguageClient[{0}]: JsonRpc.StartListening.", Id);

			jsonRpc = new JsonRpc (connection.Writer, connection.Reader);
			jsonRpc.StartListening ();
			jsonRpc.JsonSerializer.NullValueHandling = NullValueHandling.Ignore;

			LanguageClientLoggingService.Log ("LanguageClient[{0}]: Sending 'initialize' message.", Id);

			var message = new InitializeParams ();
			var result = await jsonRpc.InvokeWithParameterObjectAsync<InitializeResult> ("initialize", message);

			LanguageClientLoggingService.Log ("LanguageClient[{0}]: Initialized.", Id);

			ServerCapabilities = result.Capabilities;
		}

		public void Dispose ()
		{
			Stop ();
		}

		public bool IsSupportedDocument (Document document)
		{
			return document.FileName.HasExtension (fileExtension);
		}

		public void OpenDocument (Document document)
		{
			Runtime.AssertMainThread ();

			if (IsStarted) {
				SendOpenDocumentMessage (new DocumentToOpen (document))
					.LogFault ();
			}
		}

		Task SendOpenDocumentMessage (DocumentToOpen document)
		{
			LanguageClientLoggingService.Log ("LanguageClient[{0}]: Sending 'textDocument/didOpen'. File: '{1}'", Id, document.FileName);

			var message = new DidOpenTextDocumentParams {
				TextDocument = new TextDocumentItem {
					LanguageId = LanguageIdentifiers.GetLanguageIdentifier (document.FileName),
					Uri = document.FileName,
					Text = document.Text
				}
			};

			return jsonRpc.NotifyWithParameterObjectAsync ("textDocument/didOpen", message);
		}

		public void CloseDocument (Document document)
		{
			Runtime.AssertMainThread ();

			if (IsStarted) {
				SendCloseDocumentMessage (document.FileName)
					.LogFault ();
			}
		}

		Task SendCloseDocumentMessage (FilePath fileName)
		{
			LanguageClientLoggingService.Log ("LanguageClient[{0}]: Sending 'textDocument/didClose'. File: '{1}'", Id, fileName);

			var message = new DidCloseTextDocumentParams {
				TextDocument = new TextDocumentIdentifier {
					Uri = fileName
				}
			};

			return jsonRpc.NotifyWithParameterObjectAsync ("textDocument/didClose", message);
		}
	}
}
