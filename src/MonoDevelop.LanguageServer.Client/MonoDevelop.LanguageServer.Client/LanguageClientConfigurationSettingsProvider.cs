//
// LanguageClientConfigurationSettingsProvider.cs
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
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.LanguageServer.Client
{
	static class LanguageClientConfigurationSettingsProvider
	{
		public static JObject GetSettings (IEnumerable<string> configurationSections)
		{
			if (!AnySections (configurationSections)) {
				return null;
			}

			FilePath settingsFile = GetDefaultSettingsFile ();

			if (!File.Exists (settingsFile)) {
				return null;
			}

			string json = File.ReadAllText (settingsFile);
			return GetSettings (configurationSections, json);
		}

		public static JObject GetSettings (IEnumerable<string> configurationSections, string json)
		{
			if (!AnySections (configurationSections)) {
				return null;
			}

			JObject settings = ReadJson (json);

			var sectionsList = configurationSections
				.Select (section => section + ".")
				.ToList ();

			bool hasSettings = false;

			foreach (JProperty prop in settings.Properties ().ToList ()) {
				if (IncludedInSection (prop.Name, sectionsList)) {
					hasSettings = true;
				} else {
					settings.Remove (prop.Name);
				}
			}

			if (hasSettings) {
				return settings;
			}

			return null;
		}

		static bool IncludedInSection (string name, IEnumerable<string> sections)
		{
			return sections.Any (section => {
				return name.StartsWith (section, StringComparison.OrdinalIgnoreCase);
			});
		}

		static bool AnySections (IEnumerable<string> configurationSections)
		{
			return configurationSections?.Any () == true;
		}

		static FilePath GetDefaultSettingsFile ()
		{
			return UserProfile.Current.ConfigDir.Combine ("VSWorkspaceSettings.json");
		}

		static JObject ReadJson (string json)
		{
			using (var stringReader = new StringReader (json)) {
				using (var reader = new JsonTextReader (stringReader)) {
					return (JObject)JToken.ReadFrom (reader);
				}
			}
		}
	}
}
