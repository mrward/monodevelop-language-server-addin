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

		[JsonRpcMethod(Methods.Initialize)]
		public object Initialize(JToken arg)
		{
			Log(Methods.Initialize, arg);

			var capabilities = new ServerCapabilities();
			capabilities.TextDocumentSync = new TextDocumentSyncOptions();
			capabilities.TextDocumentSync.OpenClose = true;
			capabilities.TextDocumentSync.Change = TextDocumentSyncKind.Full;
			capabilities.CompletionProvider = new CompletionOptions();
			capabilities.CompletionProvider.ResolveProvider = false;
			capabilities.CompletionProvider.TriggerCharacters = new string[] { ",", "." };

			var result = new InitializeResult();
			result.Capabilities = capabilities;

			Initialized?.Invoke(this, new EventArgs());

			return result;
		}

		[JsonRpcMethod(Methods.TextDocumentDidOpen)]
		public void OnTextDocumentOpened(JToken arg)
		{
			Log(Methods.TextDocumentDidOpen, arg);

			var parameter = arg.ToObject<DidOpenTextDocumentParams>();
			server.OnTextDocumentOpened(parameter);
		}

		[JsonRpcMethod(Methods.TextDocumentDidClose)]
		public void OnTextDocumentClosed(JToken arg)
		{
			Log(Methods.TextDocumentDidOpen, arg);
		}

		[JsonRpcMethod(Methods.TextDocumentDidChange)]
		public void OnTextDocumentChanged(JToken arg)
		{
			Log(Methods.TextDocumentDidChange, arg);

			var parameter = arg.ToObject<DidChangeTextDocumentParams>();
			server.SendDiagnostics(parameter.TextDocument.Uri, parameter.ContentChanges[0].Text);
		}

		[JsonRpcMethod(Methods.TextDocumentCompletion)]
		public CompletionItem[] OnTextDocumentCompletion(JToken arg)
		{
			Log(Methods.TextDocumentCompletion, arg);

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

		[JsonRpcMethod(Methods.WorkspaceDidChangeConfiguration)]
		public void OnDidChangeConfiguration(JToken arg)
		{
			LoggingService.Log(Methods.WorkspaceDidChangeConfiguration, arg);

			var parameter = arg.ToObject<DidChangeConfigurationParams>();
			this.server.SendSettings(parameter);
		}

		[JsonRpcMethod(Methods.Shutdown)]
		public object Shutdown()
		{
			Log(Methods.Shutdown);

			return null;
		}

		[JsonRpcMethod(Methods.Exit)]
		public void Exit()
		{
			Log(Methods.Exit);

			server.Exit();
		}

		[JsonRpcMethod(Methods.TextDocumentReferences)]
		public Location[] OnTextDocumentReferences(JToken arg)
		{
			Log(Methods.TextDocumentReferences, arg);

			var parameter = arg.ToObject<ReferenceParams>();
			return server.FindReferences(parameter);
		}

		[JsonRpcMethod("textDocument/hover")]
		public Hover OnTextDocumentHover(JToken arg)
		{
			Log("textDocument/hover", arg);

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