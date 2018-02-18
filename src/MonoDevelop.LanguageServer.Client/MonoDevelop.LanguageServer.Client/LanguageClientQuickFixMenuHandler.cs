//
// LanguageClientQuickFixMenuHandler.cs
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

using System;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientQuickFixMenuHandler : CommandHandler
	{
		/// <summary>
		/// Gets the language client extension if it supports code actions.
		/// </summary>
		static LanguageClientTextEditorExtension GetLanguageClientExtension ()
		{
			var editor = IdeApp.Workbench.ActiveDocument?.Editor;
			var languageClient = editor?.GetContent<LanguageClientTextEditorExtension> ();

			if (languageClient?.IsCodeActionProvider == true) {
				return languageClient;
			}
			return null;
		}

		protected override async Task UpdateAsync (CommandArrayInfo info, CancellationToken cancelToken)
		{
			var languageClient = GetLanguageClientExtension ();
			if (languageClient == null) {
				return;
			}

			try {
				await AddQuickFixCommands (info, languageClient, cancelToken);
			} catch (OperationCanceledException) {
				// Ignore.
			} catch (Exception ex) {
				LoggingService.LogError ("Error creating quick fix menu.", ex); 
				info.Clear ();
				AddNoFixesAvailableCommand (info);
			}
		}

		async Task AddQuickFixCommands (CommandArrayInfo info, LanguageClientTextEditorExtension languageClient, CancellationToken token)
		{
			AddCommand (info, GettextCatalog.GetString ("Loading..."));

			var commands = await languageClient.GetCodeActions (token);

			AddCommands (info, commands);
		}

		void AddCommands (CommandArrayInfo info, LanguageClientCodeAction[] actions)
		{
			info.Clear ();

			if (actions == null || actions.Length == 0) {
				AddNoFixesAvailableCommand (info);
				return;
			}

			foreach (var action in actions) {
				AddCommand (info, action.Title, enabled: true, dataItem: action);
			}
		}

		static void AddCommand (
			CommandArrayInfo info,
			string label,
			bool enabled = false,
			object dataItem = null)
		{
			info.Add (CreateCommandInfo (label, enabled), dataItem);
		}

		static void AddNoFixesAvailableCommand (CommandArrayInfo info)
		{
			AddCommand (info, GettextCatalog.GetString ("No code fixes available"));
		}

		static CommandInfo CreateCommandInfo (string label, bool enabled = false, bool isChecked = false)
		{
			return new CommandInfo (label, enabled, isChecked);
		}

		protected override void Run (object dataItem)
		{
			var action = dataItem as LanguageClientCodeAction;
			if (action != null) {
				action.Run ();
			}
		}
	}
}
