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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientWorkspace
	{
		Dictionary<string, LanguageClientSession> sessions =
			new Dictionary<string, LanguageClientSession> (StringComparer.OrdinalIgnoreCase);

		SolutionLanguageClientSessions solutionSessions = new SolutionLanguageClientSessions ();

		public void Initialize ()
		{
		}

		public void Dispose ()
		{
		}

		public bool IsSupported (DocumentContext context)
		{
			var languageClientContext = context as LanguageClientDocumentContext;
			if (languageClientContext == null)
				return false;

			return IsSupported (languageClientContext.FileName);
		}

		public bool IsSupported (Document document)
		{
			return IsSupported (document.FileName);
		}

		public bool IsSupported (FilePath fileName)
		{
			return LanguageClientServices.ClientProvider.HasLanguageClient (fileName);
		}

		public LanguageClientSession GetSession (Document document)
		{
			return GetSession (document.Name, document.Owner as Project, true);
		}

		public LanguageClientSession GetSession (DocumentContext context)
		{
			Runtime.AssertMainThread ();

			return GetSession (context, true);
		}

		public LanguageClientSession GetSession (Document document, bool createNewSession)
		{
			return GetSession (document.Name, document.Owner as Project, createNewSession);
		}

		public LanguageClientSession GetSession (DocumentContext context, bool createNewSession)
		{
			Runtime.AssertMainThread ();

			return GetSession (context.Name, context.Project, createNewSession);
		}

		LanguageClientSession GetSession (FilePath fileName, Project project, bool createNewSession)
		{
			IContentType contentType = LanguageClientServices.ClientProvider.GetContentType (fileName);
			if (contentType.IsUnknown ()) {
				return null;
			}

			if (!TryGetSession (contentType, project, out LanguageClientSession session)) {
				if (createNewSession) {
					session = CreateSession (contentType, project);

					if (session.RootPath.IsNull) {
						sessions [session.Id] = session;
					} else {
						solutionSessions.AddSession (session);
					}
				}
			}

			return session;
		}

		bool TryGetSession (IContentType contentType, Project project, out LanguageClientSession session)
		{
			if (project?.ParentSolution != null) {
				return solutionSessions.TryGetSession (contentType, project, out session);
			}

			return sessions.TryGetValue (contentType.TypeName, out session);
		}

		LanguageClientSession CreateSession (IContentType contentType, Project project)
		{
			ILanguageClient client = LanguageClientServices.ClientProvider.GetLanguageClient (contentType);

			var session = new LanguageClientSession (client, contentType, project.SafeGetParentSolutionBaseDirectory ());
			session.Start ();

			return session;
		}

		IEnumerable<Document> GetOpenDocumentsForSession (LanguageClientSession session)
		{
			return IdeApp.Workbench.Documents.Where (document => session.IsSupportedDocument (document));
		}

		bool IsAnyDocumentOpenForSession (LanguageClientSession session)
		{
			return GetOpenDocumentsForSession (session).Any ();
		}

		public void OnDocumentOpened (LanguageClientDocumentContext context, string text)
		{
			try {
				context.Session.OpenDocument (context.FileName, text);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("Error opening document.", ex);
			}
		}

		public void OnDocumentClosed (LanguageClientDocumentContext context)
		{
			if (context.Session == null) {
				return;
			}

			if (IsAnyDocumentOpenForSession (context.Session)) {
				context.Session.CloseDocument (context.FileName);
			} else {
				ShutdownSession (context.Session).LogFault ();
			}
		}

		async Task ShutdownSession (LanguageClientSession session)
		{
			try {
				LanguageClientLoggingService.Log ("Shutting down language client[{0}]", session.Id);

				if (session.RootPath.IsNull) {
					sessions.Remove (session.Id);
				} else {
					solutionSessions.RemoveSession (session);
				}

				await session.Stop ();

				LanguageClientLoggingService.Log ("Language client[{0}] shutdown.", session.Id);
			} catch (Exception ex) {
				LanguageClientLoggingService.LogError ("Error shutting down language client.", ex);
			}
		}
	}
}
