//
// Based on:
// https://github.com/Microsoft/VSSDK-Extensibility-Samples
// LanguageServerProtocol/MockLanguageExtension/FooLanguageClient.cs
//
// Uses StandardInput and StandardOutput instead of named pipes which
// are not fully implemented on Mono.
//
// Copyright (c) Microsoft
//

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MockLanguageExtension
{
	[ContentType("foo")]
	[Export(typeof(ILanguageClient))]
	public class FooLanguageClient : ILanguageClient, ILanguageClientCustomMessage
	{
		internal const string UiContextGuidString = "DE885E15-D44E-40B1-A370-45372EFC23AA";

		private Guid uiContextGuid = new Guid(UiContextGuidString);

		public event AsyncEventHandler<EventArgs> StartAsync;

		#pragma warning disable CS0067 // The event is never used.
		public event AsyncEventHandler<EventArgs> StopAsync;
		#pragma warning restore CS0067

		public FooLanguageClient()
		{
			Instance = this;
		}

		internal static FooLanguageClient Instance
		{
			get;
			set;
		}

		internal JsonRpc Rpc
		{
			get;
			set;
		}

		public string Name => "Foo Language Extension";

		public IEnumerable<string> ConfigurationSections
		{
			get
			{
				yield return "foo";
			}
		}

		public object InitializationOptions => null;

		public IEnumerable<string> FilesToWatch => null;

		public object MiddleLayer => new MiddleLayerProvider ();

		public object CustomMessageTarget => null;

		public Task<Connection> ActivateAsync(CancellationToken token)
		{
			var info = new ProcessStartInfo();
			var programPath = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "server", @"LanguageServer.UI.exe");
			info.FileName = "mono";
			info.Arguments = programPath;
			info.WorkingDirectory = Path.GetDirectoryName(programPath);
			info.UseShellExecute = false;
			info.RedirectStandardInput = true;
			info.RedirectStandardOutput = true;

			var process = new Process();
			process.StartInfo = info;

			Connection connection = null;

			if (process.Start())
			{
				connection = new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
			}

			return Task.FromResult(connection);
		}

		public Task AttachForCustomMessageAsync(JsonRpc rpc)
		{
			Rpc = rpc;

			return Task.FromResult (0);
		}

		public async Task OnLoadedAsync()
		{
			await StartAsync?.InvokeAsync(this, EventArgs.Empty);
		}
	}
}