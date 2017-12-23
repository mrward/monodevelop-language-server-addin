//
// LanguageClientMessageService.cs
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

using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.LanguageServer.Client
{
	static class LanguageClientMessageService
	{
		public static void ShowMessage (ShowMessageParams message)
		{
			if (string.IsNullOrEmpty (message.Message)) {
				return;
			}

			Runtime.RunInMainThread (() => {
				ShowMessageInternal (message);
			});
		}

		static void ShowMessageInternal (ShowMessageParams message)
		{
			switch (message.MessageType) {
				case MessageType.Error:
					MessageService.ShowError (message.Message);
					break;
				case MessageType.Warning:
					MessageService.ShowWarning (message.Message);
					break;
				default:
					MessageService.ShowMessage (message.Message);
					break;
			}
		}

		public static MessageActionItem ShowMessage (ShowMessageRequestParams message)
		{
			MessageActionItem result = null;

			Runtime.RunInMainThread (() => {
				result = ShowMessageInternal (message);
			}).Wait ();

			return result;
		}

		static MessageActionItem ShowMessageInternal (ShowMessageRequestParams message)
		{
			if (message.Actions == null || !message.Actions.Any ()) {
				ShowMessageInternal ((ShowMessageParams)message);
				return null;
			}

			var questionMessage = new QuestionMessage (message.Message);

			foreach (MessageActionItem action in message.Actions) {
				questionMessage.Buttons.Add (new AlertButton (action.Title));
			}

			questionMessage.Icon = GetIcon (message.MessageType);

			questionMessage.Buttons.Add (AlertButton.Cancel);
			questionMessage.DefaultButton = questionMessage.Buttons.Count - 1;

			AlertButton button = MessageService.AskQuestion (questionMessage);

			int index = questionMessage.Buttons.IndexOf (button);
			if (index < message.Actions.Length) {
				return message.Actions [index];
			}

			return null;
		}

		static IconId GetIcon (MessageType messageType)
		{
			switch (messageType) {
				case MessageType.Error:
					return Stock.Error;
				case MessageType.Warning:
					return Stock.Warning;
			}

			return null;
		}
	}
}
