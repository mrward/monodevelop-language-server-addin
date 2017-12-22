//
// LanguageClientWorkspace.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.VisualStudio.LanguageServer.Client;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientWorkspace
	{
		Dictionary<string, LanguageClientSession> sessions =
			new Dictionary<string, LanguageClientSession> (StringComparer.OrdinalIgnoreCase);

		public void Initialize ()
		{
			IdeApp.Workbench.DocumentOpened += WorkbenchDocumentOpened;
		}

		public void Dispose ()
		{
			IdeApp.Workbench.DocumentOpened -= WorkbenchDocumentOpened;
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

			if (!sessions.TryGetValue (fileName.Extension, out LanguageClientSession session)) {
				session = CreateSession (fileName);
				sessions [fileName.Extension] = session;
			}

			return session;
		}

		LanguageClientSession CreateSession (FilePath fileName)
		{
			ILanguageClient client = LanguageClientServices.ClientProvider.GetLanguageClient (fileName);

			var session = new LanguageClientSession (client, fileName.Extension);
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
				}).Wait ();
			}
		}

		void AddOpenDocumentsToSession (LanguageClientSession session)
		{
			try {
				foreach (Document document in GetOpenDocumentsForSession (session)) {
					session.OpenDocument (document);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error processing after session started.", ex);
			}
		}

		IEnumerable<Document> GetOpenDocumentsForSession (LanguageClientSession session)
		{
			return IdeApp.Workbench.Documents.Where (document => session.IsSupportedDocument (document));
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
				LoggingService.LogError ("Error opening document.", ex);
			}
		}
	}
}
