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

using MonoDevelop.Core;
using Microsoft.VisualStudio.LanguageServer.Client;
using System.Collections.Generic;
using System;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientWorkspace
	{
		Dictionary<string, LanguageClientSession> sessions =
			new Dictionary<string, LanguageClientSession> (StringComparer.OrdinalIgnoreCase);

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

			var session = new LanguageClientSession (client);
			session.Start ();

			return session;
		}
	}
}
