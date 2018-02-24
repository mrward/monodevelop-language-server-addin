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
		Connection connection;
		JsonRpc jsonRpc;
		IContentType contentType;
		CancellationToken cancellationToken = CancellationToken.None;
		TextDocumentSyncKind documentSyncKind = TextDocumentSyncKind.None;
		FilePath rootPath;

		LanguageClientCompletionProvider completionProvider;
		LanguageClientExecuteCommandProvider executeCommandProvider;

		public LanguageClientSession (ILanguageClient client, IContentType contentType, FilePath rootPath)
		{
			this.client = client;
			this.contentType = contentType;
			this.rootPath = rootPath;

			client.StartAsync += OnStartAsync;
		}

		public string Id {
			get { return contentType.TypeName; }
		}

		public IContentType ContentType {
			get { return contentType; }
		}

		public FilePath RootPath {
			get { return rootPath; }
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
					IsStarted = false;
					RemoveEventHandlers ();

					Log ("Sending '{0}' message.", Methods.Shutdown);
					await jsonRpc.InvokeAsync (Methods.Shutdown);

					Log ("Sending '{0}' message.", Methods.Exit);
					bool success = await jsonRpc.InvokeAsyncWithTimeout (Methods.Exit, 1000);
					if (!success) {
						Log ("Timed out sending '{0}' message.", Methods.Exit);
					}
				}
			} catch (Exception ex) {
				Log ("Stop error: {0}", ex);
			} finally {
				Dispose ();
			}
		}

		static async Task<bool> WaitWithTimeout (Task task, int timeout)
		{
			Task result = await Task.WhenAny (task, Task.Delay (timeout));
			return result == task;
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

			connection = await client.ActivateAsync (CancellationToken.None);

			if (connection == null) {
				throw new ApplicationException ("No connection returned from ActivateAsync.");
			}

			Log ("JsonRpc.StartListening.");

			var target = new LanguageClientTarget (this);
			jsonRpc = new JsonRpc (connection.Writer, connection.Reader, target);

			jsonRpc.Disconnected += JsonRpcDisconnected;

			InitializeCustomClientProviders ();

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

			var message = CreateInitializeParams (client, rootPath);

			var result = await jsonRpc.InvokeWithParameterObjectAsync<InitializeResult> (Methods.Initialize, message);

			Log ("Initialized.", Id);

			ServerCapabilities = result.Capabilities;
			OnServerCapabilitiesChanged ();

			await SendConfigurationSettings ();
		}

		void InitializeCustomClientProviders ()
		{
			var customClient = client as ILanguageClientCustomMessage;

			completionProvider = new LanguageClientCompletionProvider (
				jsonRpc,
				customClient?.MiddleLayer as ILanguageClientCompletionProvider
			);

			executeCommandProvider = new LanguageClientExecuteCommandProvider (
				jsonRpc,
				customClient?.MiddleLayer as ILanguageClientExecuteCommandProvider
			);
		}

		static InitializeParams CreateInitializeParams (ILanguageClient client, FilePath rootPath)
		{
			int processId = 0;
			using (Process process = Process.GetCurrentProcess ()) {
				processId = process.Id;
			}

			return new InitializeParams {
				Capabilities = new ClientCapabilities (),
				InitializationOptions = client.InitializationOptions,
				ProcessId = processId,
				RootUri = rootPath.ToUri (),
				RootPath = rootPath.ToString ()
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

			if (jsonRpc != null) {
				jsonRpc.Disconnected -= JsonRpcDisconnected;
			}
		}

		void Dispose ()
		{
			try {
				if (jsonRpc != null) {
					jsonRpc.Dispose ();
					jsonRpc = null;
				}
				if (connection != null) {
					connection.Dispose ();
					connection = null;
				}
			} catch (IOException ex) {
				// Ignore.
				LanguageClientLoggingService.LogError ("JsonRpc.Dispose error.", ex);
			}
		}

		public bool IsSupportedDocument (Document document)
		{
			if (document.HasProject) {
				if (rootPath.IsNull || document.Project.ParentSolution.BaseDirectory != rootPath) {
					return false;
				}
			}

			IContentType contentTypeFound = LanguageClientServices.ClientProvider.GetContentType (document.FileName);

			return contentTypeFound.TypeName == contentType.TypeName;
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

			if (!IsCompletionProvider) {
				Log ("Get completion list is not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentCompletion, fileName);
				return null;
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentCompletion, fileName);

			var message = CreateTextDocumentPosition (fileName, completionContext);

			var result = await completionProvider.RequestCompletions (message, token);

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

			if (result is CompletionItem[] items) {
				return new CompletionList {
					IsIncomplete = false,
					Items = items
				};
			}

			if (result is CompletionList completionList) {
				return completionList;
			}

			return null;
		}

		public bool IsCompletionProvider {
			get {
				return ServerCapabilities?.CompletionProvider != null;
			}
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

		public bool IsCompletionResolveProvider {
			get {
				return ServerCapabilities?.CompletionProvider?.ResolveProvider == true;
			}
		}

		public Task<CompletionItem> ResolveCompletionItem (CompletionItem completionItem, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult<CompletionItem> (null);
			}

			if (!IsCompletionResolveProvider) {
				Log ("Resolve completions is not supported by server for '{0}'.", Methods.TextDocumentCompletionResolve);
				return Task.FromResult<CompletionItem> (null);
			}

			Log ("Sending '{0}' for '{1}'.", Methods.TextDocumentCompletionResolve, completionItem.Label);

			return completionProvider.ResolveCompletion (completionItem, token);
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

		public bool IsReferencesProvider {
			get {
				return ServerCapabilities?.ReferencesProvider == true;
			}
		}

		public Task<Location[]> GetReferences (FilePath fileName, Position position, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult (new Location[0]);
			}

			if (!IsReferencesProvider) {
				Log ("Get references is not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentReferences, fileName);
				return Task.FromResult (new Location [0]);
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

		public bool IsDefinitionProvider {
			get {
				return ServerCapabilities?.DefinitionProvider == true;
			}
		}

		public Task<Location[]> FindDefinitions (FilePath fileName, Position position, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult (new Location [0]);
			}

			if (!IsDefinitionProvider) {
				Log ("Find definitions is not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentDefinition, fileName);
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
			if (!IsStarted) {
				return Task.FromResult (new Hover ());
			}

			if (!IsHoverProvider) {
				Log ("Hover is not supported by server for '{0}'. File: '{1}'", ProtocolMethods.TextDocumentHover, fileName);
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

		public bool IsSignatureHelpProvider {
			get {
				return ServerCapabilities?.SignatureHelpProvider != null;
			}
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

			if (!IsSignatureHelpProvider) {
				Log ("Signature help is not supported by server for '{0}'. File: '{1}'", ProtocolMethods.TextDocumentSignatureHelper, fileName);
				return Task.FromResult (new SignatureHelp ());
			}

			Log ("Sending '{0}'. File: '{1}'", ProtocolMethods.TextDocumentSignatureHelper, fileName);

			var position = CreateTextDocumentPosition (fileName, completionContext);

			return jsonRpc.InvokeWithParameterObjectAsync<SignatureHelp> (
				ProtocolMethods.TextDocumentSignatureHelper,
				position,
				token);
		}

		public bool IsDocumentFormattingProvider {
			get {
				return ServerCapabilities?.DocumentFormattingProvider == true;
			}
		}

		public bool IsDocumentRangeFormattingProvider {
			get {
				return ServerCapabilities?.DocumentRangeFormattingProvider == true;
			}
		}

		public Task<TextEdit[]> FormatDocument (TextEditor editor, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult<TextEdit[]> (null);
			}

			if (!IsDocumentFormattingProvider) {
				Log ("Document formatting is not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentFormatting, editor.FileName);
				return Task.FromResult<TextEdit[]> (null);
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentFormatting, editor.FileName);

			var message = new DocumentFormattingParams {
				TextDocument = TextDocumentIdentifierFactory.Create (editor.FileName),
				Options = new FormattingOptions {
					InsertSpaces = editor.Options.TabsToSpaces,
					TabSize = editor.Options.TabSize
				}
			};

			return jsonRpc.InvokeWithParameterObjectAsync<TextEdit[]> (
				Methods.TextDocumentFormatting,
				message,
				token);
		}

		public Task<TextEdit[]> FormatDocumentRange (TextEditor editor, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult<TextEdit[]> (null);
			}

			if (!IsDocumentFormattingProvider) {
				Log ("Document formatting is not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentRangeFormatting, editor.FileName);
				return Task.FromResult<TextEdit[]> (null);
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentRangeFormatting, editor.FileName);

			var message = new DocumentRangeFormattingParams {
				TextDocument = TextDocumentIdentifierFactory.Create (editor.FileName),
				Options = new FormattingOptions {
					InsertSpaces = editor.Options.TabsToSpaces,
					TabSize = editor.Options.TabSize
				},
				Range = new Range {
					Start = editor.SelectionRegion.Begin.CreatePosition (),
					End = editor.SelectionRegion.End.CreatePosition ()
				}
			};

			return jsonRpc.InvokeWithParameterObjectAsync<TextEdit[]> (
				Methods.TextDocumentRangeFormatting,
				message,
				token);
		}

		public bool IsRenameProvider {
			get {
				return ServerCapabilities?.RenameProvider == true;
			}
		}

		public Task<WorkspaceEdit> Rename (FilePath fileName, Position position, string newName, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult<WorkspaceEdit> (null);
			}

			if (!IsRenameProvider) {
				Log ("Rename is not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentRename, fileName);
				return Task.FromResult<WorkspaceEdit> (null);
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentRename, fileName);

			var message = new RenameParams {
				TextDocument = TextDocumentIdentifierFactory.Create (fileName),
				NewName = newName,
				Position = position
			};

			return jsonRpc.InvokeWithParameterObjectAsync<WorkspaceEdit> (
				Methods.TextDocumentRename,
				message,
				token);
		}

		public bool IsCodeActionProvider {
			get {
				return ServerCapabilities?.CodeActionProvider == true;
			}
		}

		public Task<Command[]> GetCodeActions (
			FilePath fileName,
			Range range,
			Diagnostic[] diagnostics,
			CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult<Command[]> (null);
			}

			if (!IsCodeActionProvider) {
				Log ("Code actions are not supported by server for '{0}'. File: '{1}'", Methods.TextDocumentCodeAction, fileName);
				return Task.FromResult<Command[]> (null);
			}

			Log ("Sending '{0}'. File: '{1}'", Methods.TextDocumentCodeAction, fileName);

			var message = new CodeActionParams {
				TextDocument = TextDocumentIdentifierFactory.Create (fileName),
				Context = new CodeActionContext {
					Diagnostics = diagnostics
				},
				Range = range
			};

			return jsonRpc.InvokeWithParameterObjectAsync<Command[]> (
				Methods.TextDocumentCodeAction,
				message,
				token);
		}

		public Task ExecuteCommand (Command command)
		{
			if (!IsStarted) {
				return Task.CompletedTask;
			}

			Log ("Sending '{0}'.", Methods.WorkspaceExecuteCommand);

			var message = new ExecuteCommandParams {
				Command = command.CommandIdentifier,
				Arguments = command.Arguments
			};

			return executeCommandProvider.ExecuteCommand (message);
		}

		public bool IsWorkspaceSymbolProvider {
			get {
				return ServerCapabilities?.WorkspaceSymbolProvider == true;
			}
		}

		public Task<SymbolInformation[]> GetWorkspaceSymbols (string query, CancellationToken token)
		{
			if (!IsStarted) {
				return Task.FromResult<SymbolInformation[]> (null);
			}

			if (!IsWorkspaceSymbolProvider) {
				Log ("Workspace symbols are not supported by server.", Methods.WorkspaceSymbol);
				return Task.FromResult<SymbolInformation[]> (null);
			}

			Log ("Sending '{0}'.", Methods.WorkspaceSymbol);

			var message = new WorkspaceSymbolParams {
				Query = query
			};

			return jsonRpc.InvokeWithParameterObjectAsync<SymbolInformation[]> (
				Methods.WorkspaceSymbol,
				message,
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
