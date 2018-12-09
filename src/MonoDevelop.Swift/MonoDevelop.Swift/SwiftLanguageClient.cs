//
// SwiftLanguageClient.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using StreamJsonRpc;

namespace MonoDevelop.Swift
{
	[ContentType ("swift")]
	[Export (typeof (ILanguageClient))]
	class SwiftLanguageClient : ILanguageClient, ILanguageClientCustomMessage
	{
		public string Name => "Swift Language Extension";

		public IEnumerable<string> ConfigurationSections => null;

		public IEnumerable<string> FilesToWatch => null;

		public object InitializationOptions => null;

		public object CustomMessageTarget => null;

		public object MiddleLayer => null;

		public event AsyncEventHandler<EventArgs> StartAsync;

		#pragma warning disable CS0067 // The event is never used.
		public event AsyncEventHandler<EventArgs> StopAsync;
		#pragma warning restore CS0067

		/// <summary>
		/// Uses https://github.com/apple/sourcekit-lsp
		/// 
		/// git clone this repository so it sits alongside the monodevelop-language-server-addin
		/// directory in a utils subdirectory.
		/// 
		/// The sourcekit-lsp requires the path to the snapshot swift toolchain to be configured
		/// through a SOURCEKIT_TOOLCHAIN_PATH environment variable. Currently this addin
		/// will use that environment variable if defined, otherwise it will try a hard coded path:
		/// /Library/Developer/Toolchains/swift-DEVELOPMENT-SNAPSHOT-2018-12-02-a.xctoolchain/
		/// </summary>
		public Task<Connection> ActivateAsync (CancellationToken token)
		{
			string currentDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);

			string swiftLanguageServerDirectory = Path.Combine (
				currentDirectory,
				"../../../sourcekit-lsp/.build/debug");

			if (!Directory.Exists (swiftLanguageServerDirectory)) {
				throw new Exception (string.Format ("Swift language server not found at '{0}'.", swiftLanguageServerDirectory));
			}

			string toolchainPath = Environment.GetEnvironmentVariable ("SOURCEKIT_TOOLCHAIN_PATH");
			if (string.IsNullOrEmpty (toolchainPath)) {
				toolchainPath = "/Library/Developer/Toolchains/swift-DEVELOPMENT-SNAPSHOT-2018-12-02-a.xctoolchain/";
			}

			string languageServerPath = Path.Combine (swiftLanguageServerDirectory, "sourcekit-lsp");

			var info = new ProcessStartInfo {
				FileName = languageServerPath,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = swiftLanguageServerDirectory
			};

			info.EnvironmentVariables ["SOURCEKIT_TOOLCHAIN_PATH"] = toolchainPath;

			var process = new Process ();
			process.StartInfo = info;
			Connection connection = null;

			if (process.Start ()) {
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
