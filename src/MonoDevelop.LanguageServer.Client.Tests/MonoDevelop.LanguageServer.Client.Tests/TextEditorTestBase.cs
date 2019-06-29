//
// TextEditorTestBase.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.LanguageServer.Client.Tests
{
	class TextEditorTestBase
	{
		static bool firstRun = true;
		static string rootDir;

		public static string TestsRootDir {
			get {
				if (rootDir == null) {
					rootDir = Path.GetDirectoryName (typeof (TextEditorTestBase).Assembly.Location);
				}
				return rootDir;
			}
		}

		[TestFixtureSetUp]
		public async Task SetupAsync ()
		{
			if (firstRun) {
				string configRootDir = Path.Combine (TestsRootDir, "config");
				try {
					firstRun = false;
					await InternalSetupAsync (configRootDir);
				} catch (Exception) {
					// if we encounter an error, try to re create the configuration directory
					// (This takes much time, therfore it's only done when initialization fails)
					try {
						if (Directory.Exists (configRootDir))
							Directory.Delete (configRootDir, true);
						await InternalSetupAsync (rootDir);
					} catch (Exception) {
					}
				}
				await InitializeServicesAsync ();
			}
		}

		//protected virtual void InternalSetup (string rootDir)
		//{
			//Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", rootDir);
			//Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", rootDir);
			//Runtime.Initialize (true);
			//Gtk.Application.Init ();
			//DesktopService.Initialize ();
		//}

		async Task InitializeServicesAsync ()
		{
			foreach (RequireServiceAttribute attribute in Attribute.GetCustomAttributes (GetType (), typeof (RequireServiceAttribute), true)) {
				var m = typeof (ServiceProvider).GetMethod ("GetService").MakeGenericMethod (attribute.ServiceType);
				var task = (Task)m.Invoke (Runtime.ServiceProvider, new object[0]);
				await task;
			}
		}

		protected virtual Task InternalSetupAsync (string rootDir)
		{
			//Util.ClearTmpDir ();
			Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", rootDir);
			Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", rootDir);
			global::MonoDevelop.Projects.Services.ProjectService.DefaultTargetFramework
				= Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_4_0);

			return Task.CompletedTask;
		}
	}
}
