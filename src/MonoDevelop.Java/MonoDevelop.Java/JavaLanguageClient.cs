//
// JavaLanguageClient.cs
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.LanguageServer.Client;

namespace MonoDevelop.Docker
{
	[ContentType ("java")]
	[Export (typeof (ILanguageClient))]
	class JavaLanguageClient : ILanguageClient
	{
		public string Name => "Java Language Extension";

		public IEnumerable<string> ConfigurationSections {
			get {
				yield return "java";
			}
		}

		public object InitializationOptions => null;

		public IEnumerable<string> FilesToWatch => null;

		public event AsyncEventHandler<EventArgs> StartAsync;

		#pragma warning disable CS0067 // The event is never used.
		public event AsyncEventHandler<EventArgs> StopAsync;
		#pragma warning restore CS0067

		/// <summary>
		/// Requires the Java Language Support extension to be installed into Visual Studio Code
		/// https://github.com/georgewfraser/vscode-javac
		/// </summary>
		public async Task<Connection> ActivateAsync (CancellationToken token)
		{
			string currentDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);
			logFile = Path.Combine (currentDirectory, "javac.log");

			var vscodeDirectory = Path.Combine (
				Environment.GetFolderPath (
					Environment.SpecialFolder.UserProfile),
				".vscode",
				"extensions");

			if (!Directory.Exists (vscodeDirectory)) {
				throw new Exception ("Java Language Support for Visual Studio Code required.");
			}

			var extensionDirectory = Directory.GetDirectories (vscodeDirectory)
				.FirstOrDefault (m => m.Contains ("georgewfraser.vscode-javac"));

			if (extensionDirectory == null) {
				throw new Exception ("Java Language Support for Visual Studio Code required.");
			}

			string javaBinPath = "/usr/bin/java";
			if (!File.Exists (javaBinPath)) {
				throw new ApplicationException (string.Format ("Java not found at '{0}'", javaBinPath));
			}

			string fatJar = Path.Combine (extensionDirectory, "out", "fat-jar.jar");

			if (!File.Exists (fatJar)) {
				throw new ApplicationException (string.Format ("File not found '{0}'", fatJar));
			}

			IPAddress localHost = Dns.GetHostEntry ("localhost").AddressList[0];
			listener = new TcpListener (localHost, 0);
			listener.Start ();

			var endpoint = (IPEndPoint)listener.LocalEndpoint;
			int port = endpoint.Port;

			string arguments = string.Format (
				"-cp \"{0}\" -Djavacs.port={1} -Xverify:none org.javacs.Main",
				fatJar, port);

			var info = new ProcessStartInfo {
				FileName = javaBinPath,
				Arguments = arguments,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			LanguageClientLoggingService.Log ("{0} {1}", javaBinPath, arguments);

			var process = new Process ();
			process.StartInfo = info;
			Connection connection = null;

			if (process.Start ()) {
				Read (process.StandardOutput).Ignore ();
				Read (process.StandardError).Ignore ();

				var tcpClient = await listener.AcceptTcpClientAsync ();
				NetworkStream ns = tcpClient.GetStream ();

				connection = new Connection (ns, ns);
			}

			return connection;
		}

		public async Task OnLoadedAsync ()
		{
			await StartAsync?.InvokeAsync (this, EventArgs.Empty);
		}

		TcpListener listener;
		string logFile;

		async Task Read (StreamReader reader)
		{
			string result = await reader.ReadLineAsync ();
			if (result != null) {
				LanguageClientLoggingService.Log (result);
				await Read (reader);
			}
		}
	}
}
