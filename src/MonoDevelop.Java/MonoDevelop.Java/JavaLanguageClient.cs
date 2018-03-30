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
using System.Text;
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
		/// https://github.com/redhat-developer/vscode-java
		/// </summary>
		public Task<Connection> ActivateAsync (CancellationToken token)
		{
			string currentDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);

			var vscodeDirectory = Path.Combine (
				Environment.GetFolderPath (
					Environment.SpecialFolder.UserProfile),
				".vscode",
				"extensions");

			if (!Directory.Exists (vscodeDirectory)) {
				throw new Exception ("Java Language Support for Visual Studio Code required.");
			}

			var extensionDirectory = Directory.GetDirectories (vscodeDirectory)
				.FirstOrDefault (m => m.Contains ("redhat.java"));

			if (extensionDirectory == null) {
				throw new Exception ("Java Language Support for Visual Studio Code required.");
			}

			string javaBinPath = "/usr/bin/java";
			if (!File.Exists (javaBinPath)) {
				throw new ApplicationException (string.Format ("Java not found at '{0}'", javaBinPath));
			}

			string pluginsDirectory = Path.Combine (extensionDirectory, "server", "plugins");

			string launcher = Directory.GetFiles (pluginsDirectory, "org.eclipse.equinox.launcher_*.jar")
				.FirstOrDefault ();
			if (!File.Exists (launcher)) {
				throw new ApplicationException (string.Format ("Launcher not found."));
			}

			string configDirectory = Path.Combine (extensionDirectory, "server", "config_mac");

			var arguments = new StringBuilder ();
			arguments.Append ("-Declipse.application=org.eclipse.jdt.ls.core.id1 ");
			arguments.Append ("-Dosgi.bundles.defaultStartLevel=4 ");
			arguments.Append ("-Declipse.product=org.eclipse.jdt.ls.core.product ");
			arguments.Append ("-Dlog.protocol=true ");
			arguments.Append ("-Dlog.level=ALL ");
			arguments.AppendFormat ("-jar \"{0}\" ", launcher);
			arguments.AppendFormat ("-configuration \"{0}\" ", configDirectory);
			arguments.AppendFormat ("-data \"{0}\" ", currentDirectory);

			var info = new ProcessStartInfo {
				FileName = javaBinPath,
				Arguments = arguments.ToString (),
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			LanguageClientLoggingService.Log ("{0} {1}", javaBinPath, info.Arguments);

			var process = new Process ();
			process.StartInfo = info;
			Connection connection = null;

			if (process.Start ()) {
				connection = new Connection (process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
			}

			return Task.FromResult (connection);
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
