//
// BashLanguageClient.cs
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

namespace MonoDevelop.Bash
{
	[ContentType ("bash")]
	[Export (typeof (ILanguageClient))]
	class BashLanguageClient : ILanguageClient
	{
		public string Name => "Bash Language Extension";

		public IEnumerable<string> ConfigurationSections => null;

		public object InitializationOptions => null;

		public IEnumerable<string> FilesToWatch => null;

		public event AsyncEventHandler<EventArgs> StartAsync;

		#pragma warning disable CS0067 // The event is never used.
		public event AsyncEventHandler<EventArgs> StopAsync;
		#pragma warning restore CS0067

		/// <summary>
		/// Run the following from the 'bin' directory:
		/// 
		/// npm install dockerfile-language-server-nodejs
		/// </summary>
		public async Task<Connection> ActivateAsync (CancellationToken token)
		{
			string baseLanguageServerFileName = await  GetBashLanguageServerFileName (token);

			var info = new ProcessStartInfo {
				FileName = baseLanguageServerFileName,
				Arguments = "start",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			var process = new Process ();
			process.StartInfo = info;
			Connection connection = null;

			if (process.Start ()) {
				connection = new Connection (process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
			}

			return connection;
		}

		async Task<string> GetBashLanguageServerFileName (CancellationToken token)
		{
			var info = new ProcessStartInfo {
				FileName = "npm",
				Arguments = "bin -g",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			var process = new Process {
				StartInfo = info
			};
			process.Start ();

			int exitCode = await process.WaitForExitAsync (token);
			if (exitCode != 0) {
				throw new ApplicationException (string.Format ("npm '{0}' returned unexpected exit code. {1}", info.Arguments, exitCode));
			}

			string npmGlobalToolsBinPath = await process.StandardOutput.ReadToEndAsync ();
			if (npmGlobalToolsBinPath != null) {
				npmGlobalToolsBinPath = npmGlobalToolsBinPath.TrimEnd ();
				return Path.Combine (npmGlobalToolsBinPath, "bash-language-server");
			}
			return "bash-language-server";
		}

		public async Task OnLoadedAsync ()
		{
			await StartAsync?.InvokeAsync (this, EventArgs.Empty);
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
