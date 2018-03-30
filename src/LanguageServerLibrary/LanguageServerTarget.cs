//
// Based on:
// https://github.com/Microsoft/VSSDK-Extensibility-Samples
// LanguageServerProtocol/LanguageServerLibrary/LanguageServerTarget.cs
//
// Copyright (c) Microsoft
//

using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;

namespace LanguageServer
{
	public class LanguageServerTarget
	{
		private readonly LanguageServer server;

		public LanguageServerTarget(LanguageServer server)
		{
			this.server = server;
		}

		public event EventHandler Initialized;

		[JsonRpcMethod(Methods.InitializeName)]
		public object Initialize(JToken arg)
		{
			Log(Methods.InitializeName, arg);

			var capabilities = new ServerCapabilities();
			capabilities.TextDocumentSync = new TextDocumentSyncOptions();
			capabilities.TextDocumentSync.OpenClose = true;
			capabilities.TextDocumentSync.Change = TextDocumentSyncKind.Full;
			capabilities.CompletionProvider = new CompletionOptions();
			capabilities.CompletionProvider.ResolveProvider = false;
			capabilities.CompletionProvider.TriggerCharacters = new string[] { ",", "." };
			capabilities.SignatureHelpProvider = new SignatureHelpOptions ();
			capabilities.SignatureHelpProvider.TriggerCharacters = new string [] { "(" };
			capabilities.DefinitionProvider = true;
			capabilities.ReferencesProvider = true;
			capabilities.HoverProvider = true;

			var result = new InitializeResult();
			result.Capabilities = capabilities;

			return result;
		}

		[JsonRpcMethod(Methods.InitializedName)]
		public void OnInitialized(JToken arg)
		{
			Log(Methods.InitializeName, arg);

			Initialized?.Invoke(this, new EventArgs());
		}

		[JsonRpcMethod(Methods.TextDocumentDidOpenName)]
		public void OnTextDocumentOpened(JToken arg)
		{
			Log(Methods.TextDocumentDidOpenName, arg);

			var parameter = arg.ToObject<DidOpenTextDocumentParams>();
			server.OnTextDocumentOpened(parameter);
		}

		[JsonRpcMethod(Methods.TextDocumentDidCloseName)]
		public void OnTextDocumentClosed(JToken arg)
		{
			Log(Methods.TextDocumentDidCloseName, arg);

			var parameter = arg.ToObject<DidCloseTextDocumentParams>();
			server.OnTextDocumentClosed(parameter);
		}

		[JsonRpcMethod(Methods.TextDocumentDidChangeName)]
		public void OnTextDocumentChanged(JToken arg)
		{
			Log(Methods.TextDocumentDidChangeName, arg);

			var parameter = arg.ToObject<DidChangeTextDocumentParams>();
			server.SendDiagnostics(parameter.TextDocument.Uri, parameter.ContentChanges[0].Text);
		}

		[JsonRpcMethod(Methods.TextDocumentCompletionName)]
		public CompletionItem[] OnTextDocumentCompletion(JToken arg)
		{
			Log(Methods.TextDocumentCompletionName, arg);

			List<CompletionItem> items = new List<CompletionItem>();

			for (int i = 0; i < 10; i++)
			{
				var item = new CompletionItem();
				item.Label = "Item " + i;
				item.InsertText = "Item" + i;
				item.Kind = (CompletionItemKind)(i % (Enum.GetNames(typeof(CompletionItemKind)).Length) + 1);
				items.Add(item);
			}

			return items.ToArray();
		}

		[JsonRpcMethod(Methods.WorkspaceDidChangeConfigurationName)]
		public void OnDidChangeConfiguration(JToken arg)
		{
			Log(Methods.WorkspaceDidChangeConfigurationName, arg);

			var parameter = arg.ToObject<DidChangeConfigurationParams>();
			this.server.SendSettings(parameter);
		}

		[JsonRpcMethod(Methods.ShutdownName)]
		public object Shutdown()
		{
			Log(Methods.ShutdownName);

			return null;
		}

		[JsonRpcMethod(Methods.ExitName)]
		public void Exit()
		{
			Log(Methods.ExitName);

			server.Exit();
		}

		[JsonRpcMethod(Methods.TextDocumentReferencesName)]
		public Location[] OnTextDocumentReferences(JToken arg)
		{
			Log(Methods.TextDocumentReferencesName, arg);

			var parameter = arg.ToObject<ReferenceParams>();
			return server.FindReferences(parameter);
		}

		[JsonRpcMethod(Methods.TextDocumentHoverName)]
		public Hover OnTextDocumentHover(JToken arg)
		{
			Log(Methods.TextDocumentHoverName, arg);

			var contents = new List<MarkedString>();

			contents.Add(new MarkedString
			{
				Value = "Documentation"
			});
			contents.Add(new MarkedString
			{
				Value = "Summary"
			});

			return new Hover
			{
				Contents = contents.ToArray()
			};
		}

		[JsonRpcMethod(Methods.TextDocumentDefinitionName)]
		public Location[] OnTextDocumentDefinition(JToken arg)
		{
			Log(Methods.TextDocumentDefinitionName, arg);

			var parameter = arg.ToObject<TextDocumentPositionParams>();
			return server.GoToDefinition(parameter);
		}

		[JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
		public SignatureHelp OnTextDocumentSignaureHelp(JToken arg)
		{
			Log(Methods.TextDocumentSignatureHelpName, arg);

			var signatures = new List<SignatureInformation>();

			for (int i = 0; i < 2; ++i)
			{
				var signature = new SignatureInformation
				{
					Documentation = CreateMarkupContent("Signature documentation " + i),
					Label = "Signature " + i
				};

				var parameters = new List<ParameterInformation>();

				for (int j = 0; j < 3; ++j)
				{
					var parameter = new ParameterInformation
					{
						Documentation = CreateMarkupContent("Parameter documentation " + i),
						Label = "Parameter " + i
					};
				}

				signature.Parameters = parameters.ToArray();

				signatures.Add(signature);
			}

			return new SignatureHelp
			{
				Signatures = signatures.ToArray()
			};
		}

		static MarkupContent CreateMarkupContent(string text)
		{
			return new MarkupContent
			{
				Kind = MarkupKind.PlainText,
				Value = text
			};
		}

		public string GetText()
		{
			return string.IsNullOrWhiteSpace(this.server.CustomText) ? "custom text from language server target" : this.server.CustomText;
		}

		void Log(string message)
		{
			LoggingService.Log("Client: {0}", message);
		}

		void Log(string message, JToken arg)
		{
			Log(message + "\n" + arg + "\n");
		}
	}
}