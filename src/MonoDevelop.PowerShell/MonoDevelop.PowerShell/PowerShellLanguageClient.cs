//
// PowerShellLanguageClient.cs
//
// Author:
//       Adam Driscoll
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2017 Adam Driscoll
// Copyright (c) 2017 Microsoft
//
// Based on: PowerShellLSP/PowerShellLSP/PowerShellLanguageClient.cs
// From: https://github.com/adamdriscoll/powershell-lsp
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Mono.Unix;
using MonoDevelop.LanguageServer.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace MonoDevelop.PowerShell
{
	[ContentType ("PowerShell1")]
	[Export (typeof (ILanguageClient))]
	class PowerShellLanguageClient : ILanguageClient, ILanguageClientCustomMessage
	{
		public string Name => "PowerShell Language Extension";

		public IEnumerable<string> ConfigurationSections {
			get { yield return "powershell"; }
		}

		public object InitializationOptions => null;

		public IEnumerable<string> FilesToWatch => null;

		public object CustomMessageTarget => null;

		public object MiddleLayer => new PowerShellMiddleLayer ();

		public event AsyncEventHandler<EventArgs> StartAsync;

		#pragma warning disable CS0067 // The event is never used.
		public event AsyncEventHandler<EventArgs> StopAsync;
		#pragma warning restore CS0067

		public async Task<Connection> ActivateAsync (CancellationToken token)
		{
			await Task.Yield ();

			var vscodeDirectory = Path.Combine (
				Environment.GetFolderPath (
					Environment.SpecialFolder.UserProfile),
				".vscode",
				"extensions");

			if (!Directory.Exists (vscodeDirectory)) {
				throw new Exception ("PowerShell VS Code extension required.");
			}

			var extensionDirectory = Directory.GetDirectories (vscodeDirectory)
				.FirstOrDefault (m => m.Contains ("ms-vscode.powershell"));

			var script = Path.Combine (extensionDirectory, "modules", "PowerShellEditorServices" , "Start-EditorServices.ps1");

			var info = new ProcessStartInfo {
				FileName = @"/usr/local/bin/pwsh",
				Arguments = $@"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "" & '{script}' -HostName 'Visual Studio Code Host' -HostProfileId 'Microsoft.VSCode' -HostVersion '1.5.1' -AdditionalModules @('PowerShellEditorServices.VSCode') -BundledModulesPath '{extensionDirectory}/modules' -EnableConsoleRepl -LogLevel 'Diagnostic' -LogPath '{extensionDirectory}/logs/VSEditorServices.log' -SessionDetailsPath '{extensionDirectory}/logs/PSES-VS' -FeatureFlags @()""",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			var process = new Process ();
			process.StartInfo = info;

			if (process.Start ()) {
				//Wait for startup....
				string sessionFile = $@"{extensionDirectory}/logs/PSES-VS";
				var sessionInfo = await WaitForSessionFileAsync (sessionFile);

				File.Delete (sessionFile);

				var sessionInfoJObject = JsonConvert.DeserializeObject<JObject> (sessionInfo);

				var status = (string)sessionInfoJObject ["status"];
				if (status != "started") {
					LanguageClientLoggingService.Log (sessionInfoJObject.ToString ());
					var reason = (string)sessionInfoJObject ["reason"];
					throw new ApplicationException ($"Failed to start PowerShell console. {reason}");
				}

				var languageServicePipeName = (string)sessionInfoJObject ["languageServicePipeName"];
				var client = new UnixClient (languageServicePipeName);
				var stream = client.GetStream ();
				return new Connection (stream, stream);
			}

			return null;
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

		Task ILanguageClientCustomMessage.AttachForCustomMessageAsync (JsonRpc rpc)
		{
			return Task.CompletedTask;
		}

		async Task<string> WaitForSessionFileAsync (string sessionFilePath)
		{
			int remainingTries = 60;
			int delayMilliseconds = 2000;

			while (remainingTries > 0) {
				if (File.Exists (sessionFilePath)) {
					return File.ReadAllText (sessionFilePath);
				}
				await Task.Delay (delayMilliseconds);
				--remainingTries;
			}
			throw new ApplicationException ("Timed out waiting for session file to appear");
		}
	}
}
