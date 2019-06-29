//
// LanguageClientDocumentControllerExtension.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.LanguageServer.Client
{
	[Obsolete]
	class LanguageClientDocumentControllerExtension : DocumentControllerExtension
	{
		LanguageClientDocumentContext context;
		bool didOpen;

		public override Task<bool> SupportsController (DocumentController controller)
		{
			bool supported = false;
			if (controller is FileDocumentController fileController) {
				LanguageClientServices.EnsureInitialized ();
				supported = LanguageClientServices.Workspace.IsSupported (fileController.FilePath);
			}

			return Task.FromResult (supported);
		}

		public override Task Initialize (Properties status)
		{
			var fileController = Controller as FileDocumentController;
			if (fileController != null) {
				return Initialize (fileController, status);
			}

			return base.Initialize (status);
		}

		async Task Initialize (FileDocumentController fileController, Properties status)
		{
			await base.Initialize (status);

			context = new LanguageClientDocumentContext (fileController);
			context.Session = LanguageClientServices.Workspace.GetSession (context);

			if (context.Session == null) {
				LanguageClientLoggingService.LogError (string.Format ("Unable to get language client session for {0}", context.FileName));

				context = null;
				return;
			}

			if (!context.Session.IsStarted) {
				context.Session.Started += SessionStarted;
			}
		}

		protected override object OnGetContent (Type type)
		{
			if (typeof (DocumentContext).IsAssignableFrom (type))
				return context;

			return base.OnGetContent (type);
		}

		public override void Dispose ()
		{
			if (context != null) {
				context.Dispose ();

				if (context.Session != null) {
					context.Session.Started -= SessionStarted;
				}
			}

			base.Dispose ();
		}

		protected override void OnContentChanged ()
		{
			base.OnContentChanged ();

			if (!didOpen) {
				OnDocumentedOpened ();
			}
		}

		void OnDocumentedOpened ()
		{
			if (didOpen || context == null)
				return;

			if (!context.Session.IsStarted)
				return;

			TextEditor editor = context.GetEditor ();
			if (editor == null)
				return;

			didOpen = true;
			LanguageClientServices.Workspace.OnDocumentOpened (context, editor.Text);
		}

		protected override void OnClosed ()
		{
			if (context != null)
				LanguageClientServices.Workspace.OnDocumentClosed (context);

			base.OnClosed ();
		}

		void SessionStarted (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => {
				context.Session.Started -= SessionStarted;

				OnDocumentedOpened ();
			}).LogFault ();
		}
	}
}
