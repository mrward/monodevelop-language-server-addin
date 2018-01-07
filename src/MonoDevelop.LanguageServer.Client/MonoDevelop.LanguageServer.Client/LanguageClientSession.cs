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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientSession
	{
		ILanguageClient client;
		JsonRpc jsonRpc;
		string contentTypeName;
		CancellationToken cancellationToken = CancellationToken.None;
		TextDocumentSyncKind documentSyncKind = TextDocumentSyncKind.None;

		public LanguageClientSession (ILanguageClient client, string contentTypeName)
		{
			this.client = client;
			this.contentTypeName = contentTypeName;

			client.StartAsync += OnStartAsync;
		}

		public string Id {
			get { return contentTypeName; }
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

			Task.Run (async () => {
				await client.OnLoadedAsync ();
			}).LogFault ();
		}

		public async Task Stop ()
		{
			try {
				if (IsStarted) {
					Log ("Sending '{0}' message.", Methods.Shutdown);
					await jsonRpc.InvokeAsync (Methods.Shutdown);

					Log ("Sending '{0}' message.", Methods.Exit);
					await jsonRpc.InvokeAsync (Methods.Exit);
				}
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

			jsonRpc.Disconnected += JsonRpcDisconnected;

			var customClient = client as ILanguageClientCustomMessage;
			if (customClient != null) {
				Log ("Adding LanguageClientCustomMessage.");

				if (customClient.CustomMessageTarget != null) {
					jsonRpc.AddLocalRpcTarget (customClient.CustomMessageTarget);
				}

				await customClient.AttachForCustomMessageAsync (jsonRpc);
			}

			jsonRpc.StartListening ();
			jsonRpc.JsonSerializer.NullValueHandling = NullValueHandling.Ignore;

			Log ("Sending '{0}' message.", Methods.Initialize);

			var message = CreateInitializeParams (client);

			var result = await jsonRpc.InvokeWithParameterObjectAsync<InitializeResult> (Methods.Initialize, message);

			Log ("Initialized.", Id);

			ServerCapabilities = result.Capabilities;
			OnServerCapabilitiesChanged ();

			await SendConfigurationSettings ();
		}

		static InitializeParams CreateInitializeParams (ILanguageClient client)
		{
			int processId = 0;
			using (Process process = Process.GetCurrentProcess ()) {
				processId = process.Id;
			}

			return new InitializeParams {
				Capabilities = new ClientCapabilities (),
				InitializationOptions = client.InitializationOptions,
				ProcessId = processId
			};
		}

		async Task SendConfigurationSettings ()
		{
			var settings = LanguageClientConfigurationSettingsProvider.GetSettings (
				client.ConfigurationSections,
				client.GetType ());

			if (settings == null) {
				return;
			}

			Log ("Sending '{0}' message.", Methods.WorkspaceDidChangeConfiguration);

			var message = new DidChangeConfigurationParams {
				Settings = settings
			};

			await jsonRpc.NotifyWithParameterObjectAsync (Methods.WorkspaceDidChangeConfiguration, message);

			Log ("Configuration sent.", Id);
		}

		void RemoveEventHandlers ()
		{
			if (client != null) {
				client.StartAsync -= OnStartAsync;
				client = null;
			}

			try {
				if (jsonRpc != null) {
					jsonRpc.Disconnected -= JsonRpcDisconnected;
					jsonRpc.Dispose ();
					jsonRpc = null;
				}
			} catch (IOException ex) {
				// Ignore.
				LanguageClientLoggingService.LogError ("JsonRpc.Dispose error.", ex);
			}
		}

		public bool IsSupportedDocument (Document document)
		{
			IContentType contentType = LanguageClientServices.ClientProvider.GetContentType (document.FileName);
			return contentType.TypeName == contentTypeName;
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
					Uri = document.FileName.ToUri (),
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
				TextDocument = TextDocumentIdentifierFactory.Create (fileName)
			};

			return jsonRpc.NotifyWithParameterObjectAsync (Methods.TextDocumentDidClose, message);
		}

		public void OnPublishDiagnostics (PublishDiagnosticParams diagnostic)
		{
			DiagnosticsPublished?.Invoke (this, new DiagnosticsEventArgs (diagnostic));
		}

		async Task<CompletionList> GetCompletionItems (
			FilePath fileName,
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			if (!IsStarted) {
				return null;
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentCompletion, fileName);

			var message = CreateTextDocumentPosition (fileName, completionContext);

			var result = await jsonRpc.InvokeWithParameterObjectAsync<object> (Methods.TextDocumentCompletion, message, token);

			return ConvertToCompletionList (result);
		}

		/// <summary>
		/// textDocument/completion can return one of the following:
		/// CompletionList | CompletionItem[] | null
		/// </summary>
		CompletionList ConvertToCompletionList (object result)
		{
			if (result is JArray arrayResult) {
				return new CompletionList {
					IsIncomplete = false,
					Items = arrayResult.ToObject<CompletionItem[]> (jsonRpc.JsonSerializer)
				};
			}

			if (result is JObject obj) {
				return obj.ToObject<CompletionList> (jsonRpc.JsonSerializer);
			}

			return null;
		}

		public async Task<CompletionDataList> GetCompletionList (
			FilePath fileName,
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			var completionList = await GetCompletionItems (fileName, completionContext, token);

			var completionDataList = new CompletionDataList ();
			if (completionList?.Items != null) {
				completionDataList.AddRange (this, completionList.Items);
			}

			return completionDataList;
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
				TextDocument = TextDocumentIdentifierFactory.Create (fileName)
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
				TextDocument = TextDocumentIdentifierFactory.Create (fileName),
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
				TextDocument = TextDocumentIdentifierFactory.Create (fileName),
				Position = position
			};

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentDefinition, fileName);

			return jsonRpc.InvokeWithParameterObjectAsync<Location[]> (
				Methods.TextDocumentDefinition,
				message,
				token);
		}

		public bool IsHoverProvider {
			get {
				return ServerCapabilities?.HoverProvider == true;
			}
		}

		public Task<Hover> Hover (FilePath fileName, DocumentLocation location, CancellationToken token)
		{
			if (!IsStarted || !IsHoverProvider) {
				return Task.FromResult (new Hover ());
			}

			Log ("Sending '{0}'. File: '{1}'", ProtocolMethods.TextDocumentHover, fileName);

			var position = CreateTextDocumentPosition (fileName, location);

			return jsonRpc.InvokeWithParameterObjectAsync<Hover> (
				ProtocolMethods.TextDocumentHover,
				position,
				token);
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
				TextDocument = TextDocumentIdentifierFactory.Create (fileName, version),
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

		public bool IsSignatureHelpTriggerCharacter (char character)
		{
			string[] triggerCharacters = ServerCapabilities?.SignatureHelpProvider?.TriggerCharacters;

			return IsTriggerCharacter (character, triggerCharacters);
		}

		static bool IsTriggerCharacter (char character, string[] triggerCharacters)
		{
			if (triggerCharacters != null) {
				string triggerCharacterToMatch = character.ToString ();
				return triggerCharacters.Any (c => c == triggerCharacterToMatch);
			}

			return false;
		}

		public bool IsCompletionTriggerCharacter (char? character)
		{
			if (!character.HasValue) {
				return false;
			}

			string[] triggerCharacters = ServerCapabilities?.CompletionProvider?.TriggerCharacters;

			return IsTriggerCharacter (character.Value, triggerCharacters);
		}

		public Task<SignatureHelp> GetSignatureHelp (
			FilePath fileName,
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult (new SignatureHelp ());
			}

			Log ("Sending '{0}'. File: '{1}'", ProtocolMethods.TextDocumentSignatureHelper, fileName);

			var position = CreateTextDocumentPosition (fileName, completionContext);

			return jsonRpc.InvokeWithParameterObjectAsync<SignatureHelp> (
				ProtocolMethods.TextDocumentSignatureHelper,
				position,
				token);
		}

		void JsonRpcDisconnected (object sender, JsonRpcDisconnectedEventArgs e)
		{
			if (e.Exception != null) {
				Log ("JsonRpc disconnection error. Reason: {0}, Description: {1}, Error: {2}",
					e.Reason,
					e.Description,
					e.Exception);
			} else {
				Log ("JsonRpc disconnection. Reason: {0}, Description: {1}",
					e.Reason,
					e.Description);
			}
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

		void Log (string format, object arg0, object arg1, object arg2)
		{
			string message = string.Format (format, arg0, arg1, arg2);
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
