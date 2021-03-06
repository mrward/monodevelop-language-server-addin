﻿//
// LanguageClientTarget.cs
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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Core;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientTarget
	{
		LanguageClientSession session;

		public LanguageClientTarget (LanguageClientSession session)
		{
			this.session = session;
		}

		[JsonRpcMethod (Methods.WindowLogMessageName)]
		public void OnWindowLogMessage (JToken arg)
		{
			try {
				Log (Methods.WindowLogMessageName, arg);

				var message = arg.ToObject<LogMessageParams> ();
				LogMessage (message);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("OnWindowLogMessage error.", ex);
			}
		}

		void LogMessage (LogMessageParams message)
		{
			string fullMessage = string.Format ("LanguageServer[{0}]: {1}", session.Id, message.Message);

			switch (message.MessageType) {
				case MessageType.Error:
					LanguageClientOutputPad.WriteError (fullMessage);
					break;
				default:
					LanguageClientOutputPad.WriteText (fullMessage);
					break;
			}
		}

		[JsonRpcMethod (Methods.WindowShowMessageName)]
		public void OnWindowShowMessage (JToken arg)
		{
			try {
				Log (Methods.WindowShowMessageName, arg);

				var message = arg.ToObject<ShowMessageParams> ();
				LanguageClientMessageService.ShowMessage (message);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("OnWindowShowMessage error.", ex);
			}
		}

		[JsonRpcMethod (Methods.WindowShowMessageRequestName)]
		public object OnWindowShowRequestMessage (JToken arg)
		{
			try {
				Log (Methods.WindowShowMessageRequestName, arg);

				var message = arg.ToObject<ShowMessageRequestParams> ();
				return LanguageClientMessageService.ShowMessage (message);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("OnWindowShowRequestMessage error.", ex);
			}
			return null;
		}

		[JsonRpcMethod (Methods.TextDocumentPublishDiagnosticsName)]
		public void OnPublishDiagnostics (JToken arg)
		{
			try {
				Log (Methods.TextDocumentPublishDiagnosticsName, arg);

				var message = arg.ToObject<PublishDiagnosticParams> ();
				session.OnPublishDiagnostics (message);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("OnPublishDiagnostics error.", ex);
			}
		}

		[Obsolete]
		[JsonRpcMethod (Methods.WorkspaceApplyEditName)]
		public void OnWorkspaceApplyEdit (JToken arg)
		{
			try {
				Log (Methods.WorkspaceApplyEditName, arg);

				var message = arg.ToObject<ApplyWorkspaceEditParams> ();
				Runtime.RunInMainThread (() => {
					WorkspaceEditHandler.ApplyChanges (message.Edit);
				}).LogFault ();
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("OnWorkspaceApplyEdit error.", ex);
			}
		}

		void Log (string message, JToken arg)
		{
			LanguageClientLoggingService.Log("{0}\n{1}\n", message, arg);
		}
	}
}
