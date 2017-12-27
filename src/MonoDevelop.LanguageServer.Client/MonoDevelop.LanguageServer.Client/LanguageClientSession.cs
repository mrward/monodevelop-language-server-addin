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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientSession
	{
		ILanguageClient client;
		JsonRpc jsonRpc;
		string fileExtension;
		CancellationToken cancellationToken = CancellationToken.None;
		TextDocumentSyncKind documentSyncKind = TextDocumentSyncKind.None;

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
		public event EventHandler<DiagnosticsEventArgs> DiagnosticsPublished;

		/// <summary>
		/// ILanguageClient.StartAsync += StartAsync;
		/// ILanguageClient.OnLoadedAsync ();
		/// StartAsync then calls ILanguageClient.ActivateAsync;
		/// </summary>
		public void Start ()
		{
			Log ("Call OnLoadedAsync.");

			client.OnLoadedAsync ()
				.LogFault ();
		}

		public async Task Stop ()
		{
			if (!IsStarted) {
				return;
			}

			try {
				Log ("Sending '{0}' message.", Methods.Shutdown);
				await jsonRpc.InvokeAsync (Methods.Shutdown);

				Log ("Sending '{0}' message.", Methods.Exit);
				await jsonRpc.InvokeAsync (Methods.Exit);
			} catch (Exception ex) {
				Log ("Stop error: {0}", ex);
			} finally {
				IsStarted = false;
				RemoveEventHandlers ();
			}
		}

		async Task OnStartAsync (object sender, EventArgs e)
		{
			try {
				await OnStartAsync ();
				IsStarted = true;
				Started?.Invoke (this, EventArgs.Empty);
			} catch (Exception ex) {
				LogError ("OnStartAsync error.", ex);
			}
		}

		async Task OnStartAsync ()
		{
			Log ("Call ActivateAsync.");

			Connection connection = await client.ActivateAsync (CancellationToken.None);

			if (connection == null) {
				throw new ApplicationException ("No connection returned from ActivateAsync.");
			}

			Log ("JsonRpc.StartListening.");

			var target = new LanguageClientTarget (this);
			jsonRpc = new JsonRpc (connection.Writer, connection.Reader, target);
			jsonRpc.StartListening ();
			jsonRpc.JsonSerializer.NullValueHandling = NullValueHandling.Ignore;

			Log ("Sending '{0}' message.", Methods.Initialize);

			var message = new InitializeParams ();
			var result = await jsonRpc.InvokeWithParameterObjectAsync<InitializeResult> (Methods.Initialize, message);

			Log ("Initialized.", Id);

			ServerCapabilities = result.Capabilities;
			OnServerCapabilitiesChanged ();
		}

		void RemoveEventHandlers ()
		{
			if (client != null) {
				client.StartAsync -= OnStartAsync;
				client = null;
			}

			try {
				jsonRpc?.Dispose ();
				jsonRpc = null;
			} catch (IOException ex) {
				// Ignore.
				LanguageClientLoggingService.LogError ("JsonRpc.Dispose error.", ex);
			}
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
			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentDidOpen, document.FileName);

			var message = new DidOpenTextDocumentParams {
				TextDocument = new TextDocumentItem {
					LanguageId = LanguageIdentifiers.GetLanguageIdentifier (document.FileName),
					Uri = document.FileName,
					Text = document.Text
				}
			};

			return jsonRpc.NotifyWithParameterObjectAsync (Methods.TextDocumentDidOpen, message);
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
			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentDidClose, fileName);

			var message = new DidCloseTextDocumentParams {
				TextDocument = new TextDocumentIdentifier {
					Uri = fileName
				}
			};

			return jsonRpc.NotifyWithParameterObjectAsync (Methods.TextDocumentDidClose, message);
		}

		public void OnPublishDiagnostics (PublishDiagnosticParams diagnostic)
		{
			DiagnosticsPublished?.Invoke (this, new DiagnosticsEventArgs (diagnostic));
		}

		Task<CompletionItem[]> GetCompletionItems (
			FilePath fileName,
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult<CompletionItem[]> (null);
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentCompletion, fileName);

			var message = CreateTextDocumentPosition (fileName, completionContext);
			return jsonRpc.InvokeWithParameterObjectAsync<CompletionItem[]> (Methods.TextDocumentCompletion, message, token);
		}

		public async Task<CompletionDataList> GetCompletionList (
			FilePath fileName,
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			var items = await GetCompletionItems (fileName, completionContext, token);

			var completionList = new CompletionDataList ();
			completionList.AddRange (this, items);

			return completionList;
		}

		static TextDocumentPositionParams CreateTextDocumentPosition (FilePath fileName, CodeCompletionContext completionContext)
		{
			return CreateTextDocumentPosition (
				fileName,
				completionContext.TriggerLineOffset,
				completionContext.TriggerLine - 1
			);
		}

		static TextDocumentPositionParams CreateTextDocumentPosition (FilePath fileName, DocumentLocation location)
		{
			return CreateTextDocumentPosition (
				fileName,
				location.Column - 1,
				location.Line - 1
			);
		}

		static TextDocumentPositionParams CreateTextDocumentPosition (FilePath fileName, int column, int line)
		{
			return new TextDocumentPositionParams {
				Position = new Position {
					Character = column,
					Line = line
				},
				TextDocument = new TextDocumentIdentifier {
					Uri = fileName
				}
			};
		}

		public Task<Location[]> GetReferences (FilePath fileName, Position position, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult (new Location[0]);
			}

			var message = new ReferenceParams {
				Context = new ReferenceContext {
					IncludeDeclaration = true
				},
				TextDocument = new TextDocumentIdentifier {
					Uri = fileName
				},
				Position = position
			};

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentReferences, fileName);

			return jsonRpc.InvokeWithParameterObjectAsync<Location[]> (
				Methods.TextDocumentReferences,
				message,
				token);
		}

		public Task<Location[]> FindDefinitions (FilePath fileName, Position position, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult (new Location [0]);
			}

			var message = new TextDocumentPositionParams {
				TextDocument = new TextDocumentIdentifier {
					Uri = fileName
				},
				Position = position
			};

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentDefinition, fileName);

			return jsonRpc.InvokeWithParameterObjectAsync<Location[]> (
				Methods.TextDocumentDefinition,
				message,
				token);
		}

		public Task<Hover> Hover (FilePath fileName, DocumentLocation location, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult (new Hover ());
			}

			Log ("Sending '{0}'. File: '{1}'", ProtocolMethods.TextDocumentHover, fileName);

			var position = CreateTextDocumentPosition (fileName, location);
			return jsonRpc.InvokeWithParameterObjectAsync<Hover> (ProtocolMethods.TextDocumentHover, position);
		}

		public Task TextChanged (FilePath fileName, int version, TextChangeEventArgs e, TextEditor editor)
		{
			Runtime.AssertMainThread ();

			if (!IsStarted) {
				return Task.FromResult (0);
			}

			if (!IsDocumentSyncSupported) {
				Log ("Document sync not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentDidChange, fileName);
				return Task.FromResult (0);
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentDidChange, fileName);

			var message = new DidChangeTextDocumentParams {
				TextDocument = new VersionedTextDocumentIdentifier {
					Uri = fileName,
					Version = version
				},
				ContentChanges = e.CreateTextDocumentContentChangeEvents (editor, IsDocumentSyncFull)
					.ToArray ()
			};

			return jsonRpc.NotifyWithParameterObjectAsync (Methods.TextDocumentDidChange, message);
		}

		void OnServerCapabilitiesChanged ()
		{
			TextDocumentSyncKind? documentSync = ServerCapabilities?.TextDocumentSync?.Change;
			if (documentSync.HasValue) {
				documentSyncKind = documentSync.Value;
			}
		}

		bool IsDocumentSyncIncremental {
			get { return documentSyncKind == TextDocumentSyncKind.Incremental; }
		}

		bool IsDocumentSyncFull {
			get { return documentSyncKind == TextDocumentSyncKind.Full; }
		}

		bool IsDocumentSyncSupported {
			get { return documentSyncKind != TextDocumentSyncKind.None; }
		}

		void Log (string format, object arg0)
		{
			string message = string.Format (format, arg0);
			Log (message);
		}

		void Log (string format, object arg0, object arg1)
		{
			string message = string.Format (format, arg0, arg1);
			Log (message);
		}

		string GetLogMessageFormat ()
		{
			return "LanguageClient[{0}]: {1}";
		}

		void Log (string message)
		{
			string fullMessage = string.Format (GetLogMessageFormat (), Id, message);
			LanguageClientLoggingService.Log (fullMessage);
		}

		void LogError (string message, Exception ex)
		{
			string fullMessage = string.Format (GetLogMessageFormat (), Id, message);
			LanguageClientLoggingService.LogError (fullMessage, ex);
		}
	}
}
