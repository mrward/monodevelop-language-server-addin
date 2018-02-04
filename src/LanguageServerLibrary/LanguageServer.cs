//
// Based on:
// https://github.com/Microsoft/VSSDK-Extensibility-Samples
// LanguageServerProtocol/LanguageServerLibrary/LanguageServer.cs
//
// Copyright (c) Microsoft
//

using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json;

namespace LanguageServer
{
	public class LanguageServer : INotifyPropertyChanged
	{
		private int maxProblems = -1;
		private readonly JsonRpc rpc;
		private readonly LanguageServerTarget target;
		private readonly ManualResetEvent disconnectEvent = new ManualResetEvent(false);
		private Dictionary<string, DiagnosticSeverity> diagnostics;
		private TextDocumentItem textDocument = null;
		private List<TextDocumentItem> textDocuments = new List<TextDocumentItem>();

		private int counter = 100;

		public LanguageServer(Stream sender, Stream reader, Dictionary<string, DiagnosticSeverity> initialDiagnostics = null)
		{
			this.target = new LanguageServerTarget(this);
			this.rpc = JsonRpc.Attach(sender, reader, this.target);
			this.rpc.Disconnected += OnRpcDisconnected;
			this.diagnostics = initialDiagnostics;

			this.target.Initialized += OnInitialized;
		}

		public string CustomText
		{
			get;
			set;
		}

		public string CurrentSettings
		{
			get; private set;
		}

		public event EventHandler Disconnected;
		public event PropertyChangedEventHandler PropertyChanged;

		private void OnInitialized(object sender, EventArgs e)
		{
			var timer = new Timer(LogMessage, null, 0, 5 * 1000);
		}

		public void OnTextDocumentOpened(DidOpenTextDocumentParams messageParams)
		{
			this.textDocument = messageParams.TextDocument;

			lock (textDocuments) {
				textDocuments.Add (messageParams.TextDocument);
			}

			SendDiagnostics();
		}

		public void OnTextDocumentClosed(DidCloseTextDocumentParams messageParams)
		{
			lock (textDocuments) {
				textDocuments.RemoveAll (document => document.Uri == messageParams.TextDocument.Uri);
			}
		}

		public void SetDiagnostics(Dictionary<string, DiagnosticSeverity> diagnostics)
		{
			this.diagnostics = diagnostics;
		}

		public void SendDiagnostics()
		{
			if (this.textDocument == null || this.diagnostics == null)
			{
				return;
			}

			string[] lines = this.textDocument.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

			List<Diagnostic> diagnostics = new List<Diagnostic>();
			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];

				int j = 0;
				while (j < line.Length)
				{
					Diagnostic diagnostic = null;
					foreach (var tag in this.diagnostics)
					{
						diagnostic = GetDiagnostic(line, i, ref j, tag.Key, tag.Value);

						if (diagnostic != null)
						{
							break;
						}
					}

					if (diagnostic == null)
					{
						++j;
					}
					else
					{
						diagnostics.Add(diagnostic);
					}
				}
			}

			PublishDiagnosticParams parameter = new PublishDiagnosticParams();
			parameter.Uri = textDocument.Uri;
			parameter.Diagnostics = diagnostics.ToArray();

			if (this.maxProblems > -1)
			{
				parameter.Diagnostics = parameter.Diagnostics.Take(this.maxProblems).ToArray();
			}

			Log(Methods.TextDocumentPublishDiagnostics, parameter);

			this.rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnostics, parameter);
		}

		public void SendDiagnostics(string uri, string text)
		{
			if (this.diagnostics == null)
			{
				return;
			}

			string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

			List<Diagnostic> diagnostics = new List<Diagnostic>();
			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i];

				int j = 0;
				while (j < line.Length)
				{
					Diagnostic diagnostic = null;
					foreach (var tag in this.diagnostics)
					{
						diagnostic = GetDiagnostic(line, i, ref j, tag.Key, tag.Value);

						if (diagnostic != null)
						{
							break;
						}
					}

					if (diagnostic == null)
					{
						++j;
					}
					else
					{
						diagnostics.Add(diagnostic);
					}
				}
			}

			PublishDiagnosticParams parameter = new PublishDiagnosticParams();
			parameter.Uri = uri;
			parameter.Diagnostics = diagnostics.ToArray();

			if (this.maxProblems > -1)
			{
				parameter.Diagnostics = parameter.Diagnostics.Take(this.maxProblems).ToArray();
			}

			Log(Methods.TextDocumentPublishDiagnostics, parameter);

			this.rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnostics, parameter);
		}

		public Location[] FindReferences(ReferenceParams parameter)
		{
			TextDocumentItem currentTextDocument = FindDocument(parameter.TextDocument);

			if (currentTextDocument == null)
			{
				Log(string.Format("FindReferences: TextDocument.Uri does not match any document."));
				return null;
			}

			string[] lines = currentTextDocument.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

			string item = GetReferenceItem(lines, parameter.Position);
			if (string.IsNullOrEmpty(item))
			{
				Log(string.Format("FindReferences: No item to search for."));
				return null;
			}

			Log(string.Format("FindReferences: Searching for '{0}' in '{1}'", item, currentTextDocument.Uri));

			var locations = FindReferences(item, currentTextDocument.Uri, lines).ToList();

			if (textDocuments.Count > 1) {
				lock (textDocuments) {
					foreach (var document in textDocuments.Where (doc => doc != currentTextDocument)) {
						Log(string.Format("FindReferences: Searching for '{0}' in '{1}'", item, document.Uri));

						lines = document.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
						var otherLocations = FindReferences(item, document.Uri, lines);
						locations.AddRange(otherLocations);
					}
				}
			}

			return locations.ToArray();
		}

		TextDocumentItem FindDocument (TextDocumentIdentifier documentToFind)
		{
			lock (textDocuments) {
				return textDocuments.FirstOrDefault (document => document.Uri == documentToFind.Uri);
			}
		}

		static IEnumerable<Location> FindReferences(string item, string uri, string[] lines)
		{
			for (int i = 0; i < lines.Length; ++i)
			{
				string line = lines[i];
				int index = line.IndexOf(item, StringComparison.Ordinal);
				if (index >=0)
				{
					yield return new Location
					{
						Range = new Range
						{
							Start = new Position
							{
								Character = index,
								Line = i
							},
							End = new Position
							{
								Character = index + item.Length,
								Line = i
							}
						},
						Uri = uri
					};
				}
			}
		}

		public Location[] GoToDefinition(TextDocumentPositionParams parameter)
		{
			if (textDocument?.Uri != parameter.TextDocument.Uri)
			{
				Log(string.Format("GoToDefinition: TextDocument.Uri does not match."));
				return null;
			}

			string[] lines = textDocument.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

			string item = GetReferenceItem(lines, parameter.Position);
			if (string.IsNullOrEmpty(item))
			{
				Log(string.Format("GoToDefinition: No item to search for."));
				return null;
			}

			Log(string.Format("GoToDefinition: Searching for '{0}'", item));

			var locations = new List<Location>();

			for (int i = 0; i < lines.Length; ++i)
			{
				string line = lines[i];
				if (i == parameter.Position.Line)
				{
					continue;
				}

				int index = line.IndexOf(item, StringComparison.Ordinal);
				if (index >=0)
				{
					var location = new Location
					{
						Range = new Range
						{
							Start = new Position
							{
								Character = index,
								Line = i
							},
							End = new Position
							{
								Character = index + item.Length,
								Line = i
							}
						},
						Uri = textDocument.Uri
					};
					locations.Add(location);
				}
			}

			return locations.ToArray();
		}

		private string GetReferenceItem (string[] lines, Position position)
		{
			if (lines.Length < position.Line)
			{
				return null;
			}

			int positionChar = position.Character;

			string line = lines[position.Line];

			if (positionChar >= line.Length)
			{
				positionChar = line.Length - 1;
			}

			int endIndex = line.IndexOf(' ', positionChar);
			int startIndex = -1;

			if (positionChar > 0)
			{
				startIndex = line.LastIndexOf(' ', positionChar - 1);
			}

			if (endIndex == -1)
			{
				endIndex = line.Length;
			}

			return line.Substring(startIndex + 1, endIndex - startIndex - 1);
		}

		public void LogMessage(object arg)
		{
			this.LogMessage(arg, MessageType.Info);
		}

		public void LogMessage(object arg, MessageType messageType)
		{
			this.LogMessage(arg, "testing " + counter++, messageType);
		}

		public void LogMessage(object arg, string message, MessageType messageType)
		{
			LogMessageParams parameter = new LogMessageParams
			{
				Message = message,
				MessageType = messageType
			};

			Log(Methods.WindowLogMessage, parameter);

			this.rpc.NotifyWithParameterObjectAsync(Methods.WindowLogMessage, parameter);
		}

		public void ShowMessage(string message, MessageType messageType)
		{
			ShowMessageParams parameter = new ShowMessageParams
			{
				Message = message,
				MessageType = messageType
			};

			Log(Methods.WindowShowMessage, parameter);

			this.rpc.NotifyWithParameterObjectAsync(Methods.WindowShowMessage, parameter);
		}

		public async Task<MessageActionItem> ShowMessageRequestAsync(string message, MessageType messageType, string[] actionItems)
		{
			ShowMessageRequestParams parameter = new ShowMessageRequestParams
			{
				Message = message,
				MessageType = messageType,
				Actions = actionItems.Select(a => new MessageActionItem { Title = a }).ToArray()
			};

			Log(Methods.WindowShowMessageRequest, parameter);

			var response = await this.rpc.InvokeWithParameterObjectAsync<JToken>(Methods.WindowShowMessageRequest, parameter);
			return response.ToObject<MessageActionItem>();
		}

		public void SendSettings(DidChangeConfigurationParams parameter)
		{
			this.CurrentSettings = parameter.Settings.ToString();
			this.NotifyPropertyChanged(nameof(CurrentSettings));

			try
			{
				JToken parsedSettings = JToken.Parse(this.CurrentSettings);
				int newMaxProblems = parsedSettings.Children().First().Values<int>("maxNumberOfProblems").First();
				if (this.maxProblems != newMaxProblems)
				{
					this.maxProblems = newMaxProblems;
					this.SendDiagnostics();
				}
			}
			catch (Exception ex)
			{
				LoggingService.Log("Error reading settings: " + ex.Message);
			}
		}

		public void WaitForExit()
		{
			this.disconnectEvent.WaitOne();
		}

		public void Exit()
		{
			this.disconnectEvent.Set();

			Disconnected?.Invoke(this, new EventArgs());
		}

		private Diagnostic GetDiagnostic(string line, int lineOffset, ref int characterOffset, string wordToMatch, DiagnosticSeverity severity)
		{
			if ((characterOffset + wordToMatch.Length) <= line.Length)
			{
				var subString = line.Substring(characterOffset, wordToMatch.Length);
				if (subString.Equals(wordToMatch, StringComparison.OrdinalIgnoreCase))
				{
					var diagnostic = new Diagnostic();
					diagnostic.Message = "This is an " + Enum.GetName(typeof(DiagnosticSeverity), severity);
					diagnostic.Severity = severity;
					diagnostic.Range = new Range();
					diagnostic.Range.Start = new Position(lineOffset, characterOffset);
					diagnostic.Range.End = new Position(lineOffset, characterOffset + wordToMatch.Length);
					diagnostic.Code = "Test" + Enum.GetName(typeof(DiagnosticSeverity), severity);
					characterOffset = characterOffset + wordToMatch.Length;

					return diagnostic;
				}
			}

			return null;
		}

		private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
		{
			Exit();
		}

		private void NotifyPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		void Log(string message)
		{
			LoggingService.Log("Server: {0}", message);
		}

		void Log(string message, object parameter)
		{
			string json = JsonConvert.SerializeObject(parameter);
			Log(message + "\n" + json + "\n");
		}
	}
}