//
// LanguageClientWorkspace.cs
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientWorkspace
	{
		Dictionary<string, LanguageClientSession> sessions =
			new Dictionary<string, LanguageClientSession> (StringComparer.OrdinalIgnoreCase);

		public void Initialize ()
		{
			IdeApp.Workbench.DocumentOpened += WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed += WorkbenchDocumentClosed;
			IdeApp.Workspace.SolutionUnloaded += SolutionUnloaded;
		}

		public void Dispose ()
		{
			IdeApp.Workbench.DocumentOpened -= WorkbenchDocumentOpened;
			IdeApp.Workbench.DocumentClosed -= WorkbenchDocumentClosed;
			IdeApp.Workspace.SolutionUnloaded -= SolutionUnloaded;
		}

		public bool IsSupported (Document document)
		{
			return IsSupported (document.FileName);
		}

		public bool IsSupported (FilePath fileName)
		{
			return LanguageClientServices.ClientProvider.HasLanguageClient (fileName);
		}

		public LanguageClientSession GetSession (FilePath fileName)
		{
			Runtime.AssertMainThread ();

			return GetSession (fileName, true);
		}

		public LanguageClientSession GetSession (FilePath fileName, bool createNewSession)
		{
			IContentType contentType = LanguageClientServices.ClientProvider.GetContentType (fileName);
			if (contentType.IsUnknown ()) {
				throw new InvalidOperationException ("No content type for file.");
			}

			if (!sessions.TryGetValue (contentType.TypeName, out LanguageClientSession session)) {
				if (createNewSession) {
					session = CreateSession (contentType);
					sessions [session.Id] = session;
				}
			}

			return session;
		}

		LanguageClientSession CreateSession (IContentType contentType)
		{
			ILanguageClient client = LanguageClientServices.ClientProvider.GetLanguageClient (contentType);

			var session = new LanguageClientSession (client, contentType.TypeName);
			session.Started += SessionStarted;
			session.Start ();

			return session;
		}

		void SessionStarted (object sender, EventArgs e)
		{
			var session = (LanguageClientSession)sender;

			if (Runtime.IsMainThread) {
				AddOpenDocumentsToSession (session);
			} else {
				Runtime.RunInMainThread (() => {
					AddOpenDocumentsToSession (session);
				}).LogFault ();
			}
		}

		void AddOpenDocumentsToSession (LanguageClientSession session)
		{
			try {
				foreach (Document document in GetOpenDocumentsForSession (session)) {
					session.OpenDocument (document);
				}
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("Error processing after session started.", ex);
			}
		}

		IEnumerable<Document> GetOpenDocumentsForSession (LanguageClientSession session)
		{
			return IdeApp.Workbench.Documents.Where (document => session.IsSupportedDocument (document));
		}

		bool IsAnyDocumentOpenForSession (LanguageClientSession session)
		{
			return GetOpenDocumentsForSession (session).Any ();
		}

		void WorkbenchDocumentOpened (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document)) {
				LanguageClientDocumentOpened (e.Document);
			}
		}

		void LanguageClientDocumentOpened (Document document)
		{
			try {
				LanguageClientSession currentSession = GetSession (document.FileName);
				currentSession.OpenDocument (document);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("Error opening document.", ex);
			}
		}

		void WorkbenchDocumentClosed (object sender, DocumentEventArgs e)
		{
			if (IsSupported (e.Document)) {
				LanguageClientDocumentClosed (e.Document);
			}
		}

		void LanguageClientDocumentClosed (Document document)
		{
			LanguageClientSession currentSession = GetSession (document.FileName, false);

			if (currentSession == null) {
				return;
			}

			if (IsAnyDocumentOpenForSession (currentSession)) {
				currentSession.CloseDocument (document);
			} else {
				ShutdownSession (currentSession).LogFault ();
			}
		}

		async Task ShutdownSession (LanguageClientSession session)
		{
			try {
				LanguageClientLoggingService.Log ("Shutting down language client[{0}]", session.Id);

				session.Started -= SessionStarted;

				sessions.Remove (session.Id);

				await session.Stop ();

				LanguageClientLoggingService.Log ("Language client[{0}] shutdown.", session.Id);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("Error shutting down language client.", ex);
			}
		}

		void SolutionUnloaded (object sender, SolutionEventArgs e)
		{
			ShutdownAllSessions ().LogFault ();
		}

		async Task ShutdownAllSessions ()
		{
			foreach (LanguageClientSession session in sessions.Values.ToArray ()) {
				try {
					await ShutdownSession (session);
				} catch (Exception ex) {
					LanguageClientLoggingService.LogError ("Shutdown error.", ex);
				}
			}

			sessions.Clear ();
		}
	}
}
