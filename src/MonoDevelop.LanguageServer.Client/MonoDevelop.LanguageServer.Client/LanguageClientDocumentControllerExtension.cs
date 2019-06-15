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
	class LanguageClientDocumentControllerExtension : DocumentControllerExtension
	{
		LanguageClientDocumentContext context;

		public override Task<bool> SupportsController (DocumentController controller)
		{
			bool supported = false;
			if (controller is FileDocumentController fileController) {
				supported = LanguageClientServices.Workspace.IsSupported (fileController.FilePath);
			}

			return Task.FromResult (supported);
		}

		public override Task Initialize (Properties status)
		{
			var fileController = Controller as FileDocumentController;
			if (fileController != null)
				context = new LanguageClientDocumentContext (fileController);

			return base.Initialize (status);
		}

		protected override object OnGetContent (Type type)
		{
			if (typeof (DocumentContext).IsAssignableFrom (type))
				return context;

			return base.OnGetContent (type);
		}

		public override void Dispose ()
		{
			context?.Dispose ();
			base.Dispose ();
		}
	}
}
