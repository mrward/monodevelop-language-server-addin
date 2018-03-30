//
// TypeScriptLanguageClient.cs
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;

namespace MonoDevelop.TypeScript
{
	[ContentType ("typescript")]
	[Export (typeof (ILanguageClient))]
	class TypeScriptLanguageClient : ILanguageClient, ILanguageClientCustomMessage
	{
		public string Name => "TypeScript Language Extension";

		public IEnumerable<string> ConfigurationSections => null;

		public IEnumerable<string> FilesToWatch => null;

		public object InitializationOptions => null;

		public object CustomMessageTarget => null;

		public object MiddleLayer => new MiddleLayerProvider ();

		public event AsyncEventHandler<EventArgs> StartAsync;

		#pragma warning disable CS0067 // The event is never used.
		public event AsyncEventHandler<EventArgs> StopAsync;
		#pragma warning restore CS0067

		/// <summary>
		/// Uses https://github.com/sourcegraph/javascript-typescript-langserver
		/// 
		/// git clone this repository so it sits alongside the monodevelop-language-server-addin
		/// directory.
		/// 
		/// Build the language server using:
		/// 
		/// npm install
		/// npm run build
		/// </summary>
		public Task<Connection> ActivateAsync (CancellationToken token)
		{
			string currentDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);

			string typeScriptLanguageServerDirectory = Path.Combine (
				currentDirectory,
				"../../../javascript-typescript-langserver/lib/");

			if (!Directory.Exists (typeScriptLanguageServerDirectory)) {
				throw new Exception (string.Format ("TypeScript language server not found at '{0}'.", typeScriptLanguageServerDirectory));
			}

			string logFile = Path.Combine (currentDirectory, "tsserver.log");

			string script = Path.Combine (typeScriptLanguageServerDirectory, "language-server-stdio");
			string arguments = string.Format ("--inspect=18304 \"{0}\" --trace --logfile \"{1}\"", script, logFile);

			var info = new ProcessStartInfo {
				FileName = "node",
				Arguments = arguments,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = typeScriptLanguageServerDirectory
			};

			var process = new Process ();
			process.StartInfo = info;
			Connection connection = null;

			if (process.Start()) {
				connection = new Connection (process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
			}

			return Task.FromResult (connection);
		}

		public Task AttachForCustomMessageAsync (JsonRpc rpc)
		{
			return Task.CompletedTask;
		}

		public Task OnLoadedAsync ()
		{
			return StartAsync?.InvokeAsync (this, EventArgs.Empty);
		}

		public Task OnServerInitializedAsync ()
		{
			return Task.CompletedTask;
		}

		public Task OnServerInitializeFailedAsync (Exception e)
		{
			return Task.CompletedTask;
		}
	}
}
