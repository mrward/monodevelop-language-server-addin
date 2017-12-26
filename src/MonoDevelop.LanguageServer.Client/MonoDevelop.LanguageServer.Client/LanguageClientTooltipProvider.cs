//
// LanguageClientTooltipProvider.cs
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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientTooltipProvider : TooltipProvider
	{
		public override async Task<TooltipItem> GetItem (
			TextEditor editor,
			DocumentContext ctx,
			int offset,
			CancellationToken token = default (CancellationToken))
		{
			try {
				LanguageClientSession session = LanguageClientServices.Workspace.GetSession (editor.FileName, false);

				if (session != null) {
					DocumentLocation location = editor.OffsetToLocation (offset);
					Hover result = await session.Hover (ctx.Name, location, token);
					return CreateTooltipItem (editor, result);
				}
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("TooltipProvider error.", ex);
			}

			return null;
		}

		TooltipItem CreateTooltipItem (TextEditor editor, Hover result)
		{
			if (result?.Contents?.Length > 0) {
				return new TooltipItem (
					CreateTooltipInformation (result.Contents),
					editor.GetTextSegment (result.Range)
				);
			}

			return null;
		}

		TooltipInformation CreateTooltipInformation (object[] contents)
		{
			var tooltipInfo = new TooltipInformation ();
			tooltipInfo.SummaryMarkup = EscapeMarkup (GetSummaryMarkup (contents));
			tooltipInfo.SignatureMarkup = EscapeMarkup (GetSignatureMarkup (contents));
			return tooltipInfo;
		}

		static string EscapeMarkup (string text)
		{
			return GLib.Markup.EscapeText (text ?? string.Empty);
		}

		static string GetSummaryMarkup (object[] contents)
		{
			if (contents.Length > 1) {
				return GetStringFromMarkedString (contents [1]);
			}

			return string.Empty;
		}

		static string GetSignatureMarkup (object[] contents)
		{
			if (contents.Length > 0) {
				return GetStringFromMarkedString (contents [0]);
			}

			return string.Empty;
		}

		static string GetStringFromMarkedString (object content)
		{
			var markedString = content as MarkedString;
			if (markedString != null) {
				return markedString.Value;
			}

			if (content != null) {
				return content.ToString ();
			}

			return string.Empty;
		}

		public override Window CreateTooltipWindow (
			TextEditor editor,
			DocumentContext ctx,
			TooltipItem item,
			int offset,
			Xwt.ModifierKeys modifierState)
		{
			var result = new TooltipInformationWindow ();
			result.ShowArrow = true;
			result.AddOverload ((TooltipInformation)item.Item);
			result.RepositionWindow ();

			return result;
		}
	}
}
