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
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.TypeScript
{
	[ContentType ("typescript")]
	[Export (typeof (ILanguageClient))]
	class TypeScriptLanguageClient : ILanguageClient
	{
		public string Name => "TypeScript Language Extension";

		public IEnumerable<string> ConfigurationSections => null;

		public IEnumerable<string> FilesToWatch => null;

		public object InitializationOptions => null;

		public event AsyncEventHandler<EventArgs> StartAsync;

		#pragma warning disable CS0067 // The event is never used.
		public event AsyncEventHandler<EventArgs> StopAsync;
		#pragma warning restore CS0067

		/// <summary>
		/// In the 'bin' directory at the GitHub repository root run:
		/// 
		/// 'npm install typescript-language-server'
		/// 
		/// to install the TypeScript language server.
		/// </summary>
		public Task<Connection> ActivateAsync (CancellationToken token)
		{
			string currentDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);
			string typeScriptLanguageServerDirectory = Path.Combine (
				currentDirectory,
				"node_modules/typescript-language-server/lib");

			if (!Directory.Exists (typeScriptLanguageServerDirectory)) {
				throw new Exception (string.Format ("TypeScript language server not found at '{0}'.", typeScriptLanguageServerDirectory));
			}

			string logFile = Path.Combine (currentDirectory, "tsserver.log");

			string script = Path.Combine (typeScriptLanguageServerDirectory, "cli.js");
			string tsserverPath = Path.Combine (typeScriptLanguageServerDirectory, "../node_modules/typescript/bin/tsserver");
			string arguments = string.Format ("\"{0}\" --stdio --tsserver-path \"{1}\" --tsserver-log-verbosity normal --tsserver-log-file \"{2}\"", script, tsserverPath, logFile);

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

		public Task OnLoadedAsync ()
		{
			return StartAsync?.InvokeAsync (this, EventArgs.Empty);
		}
	}
}
