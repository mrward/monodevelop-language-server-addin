//
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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Refactoring;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientTextEditorExtension : CompletionTextEditorExtension
	{
		LanguageClientSession session;
		FilePath fileName;
		List<IErrorMarker> errorMarkers = new List<IErrorMarker> ();
		int documentVersion;

		public override bool IsValidInContext (DocumentContext context)
		{
			return LanguageClientServices.Workspace.IsSupported (context.Name);
		}

		protected override void Initialize ()
		{
			fileName = DocumentContext.Name;

			session = LanguageClientServices.Workspace.GetSession (fileName);
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

			Runtime.RunInMainThread (() => {
				ShowDiagnostics (e.Diagnostics);
			});
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

				var completionList = await session.GetCompletionList (fileName, completionContext, token);

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
			if (session.IsReferencesProvider) {
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
			renamer.RenameOccurrences (fileName, Editor.CaretLocation).Ignore ();
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
			WordAtPosition word = Editor.GetWordAtPosition (Editor.CaretLine, Editor.CaretColumn);
			return word.IsEmpty;
		}

		public override Task<ParameterHintingResult> HandleParameterCompletionAsync (
			CodeCompletionContext completionContext,
			char completionChar,
			CancellationToken token = default (CancellationToken))
		{
			if (ShouldTriggerParameterCompletion (completionChar)) {
				try {
					return GetParameterCompletionAsync (completionContext, token);
				} catch (Exception ex) {
					LanguageClientLoggingService.LogError ("HandleParameterCompletionAsync error.", ex);
				}
			}
			return base.HandleParameterCompletionAsync (completionContext, completionChar, token);
		}

		bool ShouldTriggerParameterCompletion (char completionChar)
		{
			if (!session.IsSignatureHelpProvider) {
				return false;
			}

			return session.IsSignatureHelpTriggerCharacter (completionChar);
		}

		async Task<ParameterHintingResult> GetParameterCompletionAsync (
			CodeCompletionContext completionContext,
			CancellationToken token)
		{
			SignatureHelp signatureHelp = await session.GetSignatureHelp (fileName, completionContext, token);

			if (signatureHelp == null || !signatureHelp.Signatures.Any ()) {
				return ParameterHintingResult.Empty;
			}

			var parameterDataItems = signatureHelp
				.Signatures
				.Select (signature => new LanguageClientParameterHintingData (signature) as ParameterHintingData)
				.ToList ();

			return new ParameterHintingResult (parameterDataItems, completionContext.TriggerOffset);
		}
	}
}
