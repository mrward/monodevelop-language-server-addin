﻿//
// LanguageServerTextEditorExtension.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LanguageServerProtocol = Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Refactoring;

namespace MonoDevelop.LanguageServer.Client
{
	[Obsolete]
	class LanguageClientTextEditorExtension : CompletionTextEditorExtension
	{
		LanguageClientSession session;
		FilePath fileName;
		List<IErrorMarker> errorMarkers = new List<IErrorMarker> ();
		int documentVersion;
		Diagnostic[] currentDiagnostics;

		public override bool IsValidInContext (DocumentContext context)
		{
			return LanguageClientServices.Workspace.IsSupported (context);
		}

		protected override void Initialize ()
		{
			var context = (LanguageClientDocumentContext)DocumentContext;
			fileName = context.FileName;

			session = context.Session;
			session.DiagnosticsPublished += OnDiagnostics;

			Editor.TextChanged += TextChanged;

			base.Initialize ();
		}

		public override void Dispose ()
		{
			if (session != null) {
				session.DiagnosticsPublished -= OnDiagnostics;
				session = null;
			}

			if (Editor != null) {
				Editor.TextChanged -= TextChanged;
			}

			base.Dispose ();
		}

		void OnDiagnostics (object sender, DiagnosticsEventArgs e)
		{
			if (e.Uri == null || !(fileName.ToUri () == e.Uri)) {
				return;
			}

			currentDiagnostics = e.Diagnostics;

			Runtime.RunInMainThread (() => {
				ShowDiagnostics (e.Diagnostics);
			}).LogFault ();
		}

		void ShowDiagnostics (Diagnostic[] diagnostics)
		{
			ClearDiagnostics ();

			foreach (Error error in diagnostics.Select (diagnostic => diagnostic.CreateError ())) {
				IErrorMarker marker = TextMarkerFactory.CreateErrorMarker (Editor, error);
				Editor.AddMarker (marker);
				errorMarkers.Add (marker);
			}
		}

		void ClearDiagnostics ()
		{
			errorMarkers.ForEach (error => Editor.RemoveMarker (error));
			errorMarkers.Clear ();
		}

		public async override Task<ICompletionDataList> HandleCodeCompletionAsync (
			CodeCompletionContext completionContext,
			CompletionTriggerInfo triggerInfo,
			CancellationToken token = default (CancellationToken))
		{
			if (Editor.EditMode == EditMode.TextLink) {
				return null;
			}

			if (!session.IsCompletionProvider) {
				return null;
			}

			try {
				WordAtPosition word = Editor.GetWordAtPosition (completionContext);

				if (!ShouldTriggerCompletion (word, completionContext, triggerInfo)) {
					return null;
				}

				var completionList = await session.GetCompletionList (fileName, completionContext, this, token);

				if (!word.IsEmpty) {
					completionList.TriggerWordLength = word.Length;
					completionContext.TriggerLineOffset = word.StartColumn;
				}

				return completionList;
			} catch (OperationCanceledException) {
				// Ignore.
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("HandleCodeCompletionAsync error.", ex);
			}

			return null;
		}

		bool ShouldTriggerCompletion (
			WordAtPosition word,
			CodeCompletionContext completionContext,
			CompletionTriggerInfo triggerInfo)
		{
			switch (triggerInfo.CompletionTriggerReason) {
				case CompletionTriggerReason.CharTyped:
					return ShouldTriggerCompletionOnCharTyped (word, completionContext, triggerInfo);
				case CompletionTriggerReason.BackspaceOrDeleteCommand:
					return ShouldTriggerCompletionAtPosition (word, completionContext);
				default:
					// Always trigger when Ctrl+Space typed.
					return true;
			}
		}

		bool ShouldTriggerCompletionOnCharTyped (
			WordAtPosition word,
			CodeCompletionContext completionContext,
			CompletionTriggerInfo triggerInfo)
		{
			if (session.IsCompletionTriggerCharacter (triggerInfo.TriggerCharacter)) {
				return true;
			}

			return ShouldTriggerCompletionAtPosition (word, completionContext);
		}

		static bool ShouldTriggerCompletionAtPosition (
			WordAtPosition word,
			CodeCompletionContext completionContext)
		{
			if (word.IsEmpty) {
				// No word near caret - do not trigger code completion.
				return false;
			} else if (word.EndColumn != completionContext.TriggerLineOffset) {
				// Not at the end of the word. For example, a space was typed after
				// the end of the word
				return false;
			}

			return true;
		}

		[CommandUpdateHandler (RefactoryCommands.FindReferences)]
		[CommandUpdateHandler (EditCommands.Rename)]
		void EnableFindReferences (CommandInfo info)
		{
			if (session.IsReferencesProvider || session.IsRenameProvider) {
				EnableCommandsWhenWordAtCaretPosition (info);
			} else {
				info.Enabled = false;
			}
		}

		[CommandHandler (RefactoryCommands.FindReferences)]
		void FindReferences ()
		{
			var finder = new LanguageClientReferencesFinder (Editor, session);
			finder.FindReferences (fileName, Editor.CaretLocation).Ignore ();
		}

		[CommandHandler (EditCommands.Rename)]
		void Rename ()
		{
			var renamer = new LanguageClientReferencesFinder (Editor, session);

			if (session.IsRenameProvider) {
				string newName = PromptForNewNameForRename ();
				if (newName != null) {
					renamer.Rename (fileName, Editor.CaretLocation, newName).Ignore ();
				}
			} else {
				renamer.RenameOccurrences (fileName, Editor.CaretLocation).Ignore ();
			}
		}

		string PromptForNewNameForRename ()
		{
			WordAtPosition word = Editor.GetWordAtCaret ();
			if (word.IsEmpty || word.IsInvalid) {
				return null;
			}

			return RenameItemDialog.PromptForNewName (word.Text);
		}

		void TextChanged (object sender, TextChangeEventArgs e)
		{
			try {
				documentVersion++;
				session.TextChanged (fileName, documentVersion, e, Editor)
					.LogFault ();
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("TextChanged error.", ex);
			}
		}

		[CommandUpdateHandler (RefactoryCommands.GotoDeclaration)]
		void EnableGoToDeclaration (CommandInfo info)
		{
			if (session.IsDefinitionProvider) {
				EnableCommandsWhenWordAtCaretPosition (info);
			} else {
				info.Enabled = false;
			}
		}

		[CommandHandler (RefactoryCommands.GotoDeclaration)]
		void GoToDeclaration ()
		{
			var finder = new LanguageClientDeclarationFinder (Editor, session);
			finder.OpenDeclaration (fileName, Editor.CaretLocation).Ignore ();
		}

		void EnableCommandsWhenWordAtCaretPosition (CommandInfo info)
		{
			info.Enabled = !IsWordAtCurrentCaretPosition ();
		}

		bool IsWordAtCurrentCaretPosition ()
		{
			WordAtPosition word = Editor.GetWordAtCaret ();
			return word.IsEmpty;
		}

		public override Task<ParameterHintingResult> HandleParameterCompletionAsync (
			CodeCompletionContext completionContext,
			SignatureHelpTriggerInfo triggerInfo,
			CancellationToken token = default (CancellationToken))
		{
			if (ShouldTriggerParameterCompletion (triggerInfo)) {
				try {
					return GetParameterCompletionAsync (completionContext, token);
				} catch (Exception ex) {
					LanguageClientLoggingService.LogError ("HandleParameterCompletionAsync error.", ex);
				}
			}
			return base.HandleParameterCompletionAsync (completionContext, triggerInfo, token);
		}

		bool ShouldTriggerParameterCompletion (SignatureHelpTriggerInfo triggerInfo)
		{
			if (!triggerInfo.TriggerCharacter.HasValue) {
				return false;
			}

			if (!session.IsSignatureHelpProvider) {
				return false;
			}

			return session.IsSignatureHelpTriggerCharacter (triggerInfo.TriggerCharacter.Value);
		}

		async Task<ParameterHintingResult> GetParameterCompletionAsync (
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			SignatureHelp signatureHelp = await session.GetSignatureHelp (fileName, completionContext, token);

			if (signatureHelp?.Signatures == null || !signatureHelp.Signatures.Any ()) {
				return ParameterHintingResult.Empty;
			}

			var parameterDataItems = signatureHelp
				.Signatures
				.Select (signature => new LanguageClientParameterHintingData (signature) as ParameterHintingData)
				.ToList ();

			return new ParameterHintingResult (parameterDataItems) {
				ApplicableSpan = new Microsoft.CodeAnalysis.Text.TextSpan (completionContext.TriggerOffset, 0)
			};
		}

		[CommandUpdateHandler (CodeFormattingCommands.FormatBuffer)]
		void EnableFormatDocument (CommandInfo info)
		{
			if (Editor.IsSomethingSelected && session.IsDocumentRangeFormattingProvider) {
				info.Text = GettextCatalog.GetString ("_Format Selection");
			} else {
				info.Enabled = session.IsDocumentFormattingProvider;
			}
		}

		[CommandHandler (CodeFormattingCommands.FormatBuffer)]
		void FormatDocument ()
		{
			var formatter = new DocumentFormatter (Editor, session);
			formatter.FormatDocument ().Ignore ();
		}

		internal bool IsCodeActionProvider {
			get { return session.IsCodeActionProvider; }
		}

		internal async Task<LanguageClientCodeAction[]> GetCodeActions (CancellationToken token)
		{
			try {
				Range range = Editor.GetCodeActionRange ();
				Diagnostic[] diagnostics = GetDiagnostics (range);
				LanguageServerProtocol.Command[] commands = await session.GetCodeActions (Editor.FileName, range, diagnostics, token);
				if (commands != null) {
					return commands.Select (command => new LanguageClientCodeAction (session, command))
						.ToArray ();
				}
				return null;
			} catch (OperationCanceledException) {
				// Ignore.
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("GetCodeActions error.", ex);
			}
			return null;
		}

		Diagnostic[] GetDiagnostics (Range range)
		{
			if (currentDiagnostics == null) {
				return null;
			}

			return currentDiagnostics
				.Where (diagnostic => OverlappingRange (range, diagnostic.Range))
				.ToArray ();
		}

		static bool OverlappingRange (Range range1, Range range2)
		{
			if (IsInside (range1, range2.Start) || IsInside (range1, range2.End)) {
				return true;
			} else if (IsInside (range2, range1.Start) || IsInside (range2, range1.End)) {
				return true;
			}
			return false;
		}

		static bool IsInside (Range range, Position end)
		{
			if (end.Line >= range.Start.Line && end.Line <= range.End.Line) {
				if (end.Line == range.Start.Line && end.Line == range.End.Line) {
					return (end.Character >= range.Start.Character) &&
						(end.Character <= range.End.Character);
				} else if (end.Line == range.Start.Line) {
					return end.Character <= range.Start.Character;
				} else if (end.Line == range.End.Line) {
					return end.Character <= range.End.Character;
				}
				return true;
			}
			return false;
		}
	}
}
