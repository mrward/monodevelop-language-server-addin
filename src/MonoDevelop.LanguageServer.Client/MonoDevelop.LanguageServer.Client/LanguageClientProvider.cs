//
// LanguageClientProvider.cs
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;

namespace MonoDevelop.LanguageServer.Client
{
	[Export]
	class LanguageClientProvider
	{
		IFileExtensionRegistryService2 fileExtensionRegistry;
		List<ILanguageClient> clients;

		Dictionary<string, ILanguageClient> contentTypeMappings =
			new Dictionary<string, ILanguageClient> (StringComparer.OrdinalIgnoreCase);

		[ImportingConstructor]
		public LanguageClientProvider (
			[Import] IFileExtensionRegistryService2 fileExtensionRegistry,
			[ImportMany (typeof (ILanguageClient))] IEnumerable<ILanguageClient> clients)
		{
			this.fileExtensionRegistry = fileExtensionRegistry;
			this.clients = clients.ToList ();
		}

		public IEnumerable<ILanguageClient> Clients {
			get { return clients; }
		}

		public void Initialize ()
		{
			GetContentTypeMappings ();
		}

		void GetContentTypeMappings ()
		{
			foreach (ILanguageClient client in clients) {
				foreach (var contentType in client.GetType ().GetCustomAttributes<ContentTypeAttribute> ()) {
					contentTypeMappings [contentType.ContentTypes] = client;
				}
			}
		}

		public bool HasLanguageClient (FilePath fileName)
		{
			ILanguageClient client = GetLanguageClient (fileName);
			return client != null;
		}

		public ILanguageClient GetLanguageClient (FilePath fileName)
		{
			IContentType contentType = GetContentType (fileName);
			return GetLanguageClient (contentType);
		}

		public ILanguageClient GetLanguageClient (IContentType contentType)
		{
			if (contentType.IsUnknown ()) {
				return null;
			}

			if (contentTypeMappings.TryGetValue (contentType.TypeName, out ILanguageClient client)) {
				return client;
			}

			return null;
		}

		public IContentType GetContentType (FilePath fileName)
		{
			return fileExtensionRegistry.GetContentTypeForFileNameOrExtension (fileName.FileName);
		}

		public void LogClientsFound ()
		{
			if (!clients.Any ()) {
				LanguageClientLoggingService.Log ("No LanguageClients found.");
			}

			LanguageClientLoggingService.Log ("LanguageClients:");

			foreach (KeyValuePair<string, ILanguageClient> mapping in contentTypeMappings) {
				LanguageClientLoggingService.Log (
					"    Name: '{0}', ContentType: '{1}'",
					mapping.Value.Name,
					mapping.Key);
			}
		}
	}
}
