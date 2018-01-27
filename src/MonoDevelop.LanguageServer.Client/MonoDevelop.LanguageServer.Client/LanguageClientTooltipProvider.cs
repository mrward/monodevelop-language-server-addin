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
				LanguageClientSession session = LanguageClientServices.Workspace.GetSession (ctx, false);

				if (session != null) {
					DocumentLocation location = editor.OffsetToLocation (offset);
					Hover result = await session.Hover (ctx.Name, location, token);
					return CreateTooltipItem (editor, result);
				}
			} catch (OperationCanceledException) {
				// Ignore.
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("TooltipProvider error.", ex);
			}

			return null;
		}

		TooltipItem CreateTooltipItem (TextEditor editor, Hover result)
		{
			if (result?.Contents?.Length > 0) {
				var tooltipInfo = CreateTooltipInformation (result.Contents);

				if (!tooltipInfo.IsEmpty) {
					return new TooltipItem (
						tooltipInfo,
						editor.GetTextSegment (result.Range)
					);
				}
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

		public override void ShowTooltipWindow (
			TextEditor editor,
			Window tipWindow,
			TooltipItem item,
			Xwt.ModifierKeys modifierState,
			int mouseX,
			int mouseY)
		{
			if (item.Offset == -1) {
				var tooltipWindow = (TooltipInformationWindow)tipWindow;
				ShowTooltipWindowAtMouseLocation (editor, tooltipWindow, mouseX, mouseY);
			} else {
				base.ShowTooltipWindow (editor, tipWindow, item, modifierState, mouseX, mouseY);
			}
		}

		/// <summary>
		/// No text range returned from the language server so the tooltip will
		/// be shown based on the mouse cursor position. The arrow from the
		/// tooltip should be pointing to the mouse cursor.
		/// </summary>
		void ShowTooltipWindowAtMouseLocation (
			TextEditor editor,
			TooltipInformationWindow tooltipWindow,
			int mouseX,
			int mouseY)
		{
			// mouseX here does not seem to produce the correct text editor column
			// so only Point.Y is used from the TextEditor's LocationToPoint. Point.X
			// is incorrect and cannot be used to determine the tooltip rectangle
			// location.
			DocumentLocation location = editor.PointToLocation (mouseX, mouseY);
			Xwt.Point point = editor.LocationToPoint (location);

			// The target rectangle should be a segment of text in the text editor. Since
			// this does not exist the width of the tooltip window is used as the rectangle
			// width and the X position is taken from the mouseX but shifted to the left by
			// half the width of the tooltip window so the middle of the tooltip window
			// appears under the mouse position so the arrow from the tooltip should be
			// pointing to where the mouse cursor is. The mouseX and mouseY are captured by
			// the tooltip provider at the time the tooltip is initially requested and so
			// this is not necessarily the current mouse position.
			var targetRectangle = new Xwt.Rectangle (
				mouseX - (tooltipWindow.Width / 2), // Arrow from tooltip should point to mouse cursor.
				point.Y, // The top of the line where the mouse cursor is
				tooltipWindow.Width,
				editor.GetLineHeight (editor.CaretLine)
			);

			tooltipWindow.ShowPopup (editor, targetRectangle, PopupPosition.Top);
		}
	}
}
