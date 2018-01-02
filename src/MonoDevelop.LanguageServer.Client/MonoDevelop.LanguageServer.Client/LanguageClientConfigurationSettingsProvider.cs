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
using System.Reflection;
using MonoDevelop.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonoDevelop.LanguageServer.Client
{
	static class LanguageClientConfigurationSettingsProvider
	{
		public static readonly string VSWorkspaceJsonFileName = "VSWorkspaceSettings.json";

		public static JObject GetSettings (IEnumerable<string> configurationSections, Type type)
		{
			if (!AnySections (configurationSections)) {
				return null;
			}

			FilePath settingsFile = GetDefaultSettingsFile ();

			if (!File.Exists (settingsFile)) {
				return null;
			}

			string json = File.ReadAllText (settingsFile);
			return GetSettings (type, configurationSections, json);
		}

		public static JObject GetSettings (IEnumerable<string> configurationSections, string json)
		{
			if (!AnySections (configurationSections)) {
				return null;
			}

			return GetSettings (configurationSections, json, null);
		}

		static JObject GetSettings (
			IEnumerable<string> configurationSections,
			string json,
			JObject defaultSettings)
		{
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

			if (hasSettings && defaultSettings != null) {
				var mergeSettings = new JsonMergeSettings {
					MergeArrayHandling = MergeArrayHandling.Merge
				};
				defaultSettings.Merge (settings, mergeSettings);

				return defaultSettings.GroupByParentSection (configurationSections);
			} else if (hasSettings) {
				return settings.GroupByParentSection (configurationSections);
			} else if (defaultSettings != null) {
				return defaultSettings.GroupByParentSection (configurationSections);
			}

			return null;
		}

		static JObject GroupByParentSection (this JObject settings, IEnumerable<string> configurationSections)
		{
			var sectionProperties = configurationSections
				.Select (section => new JProperty (section, new JObject ()))
				.ToList ();

			foreach (JProperty prop in settings.Properties ().ToList ()) {
				AddToParentSection (prop, sectionProperties);
			}

			return new JObject (sectionProperties);
		}

		static void AddToParentSection (JProperty prop, List<JProperty> sections)
		{
			foreach (JProperty section in sections) {
				if (prop.Name.StartsWith (section.Name + ".", StringComparison.OrdinalIgnoreCase)) {
					string newPropertyName = prop.Name.Substring (section.Name.Length + 1);
					var newProperty = new JProperty (newPropertyName, prop.Value);
					var jobject = (JObject)section.First;
					jobject.Add (newProperty);
				}
			}
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
			return UserProfile.Current.ConfigDir.Combine (VSWorkspaceJsonFileName);
		}

		static JObject ReadJson (string json)
		{
			using (var stringReader = new StringReader (json)) {
				return ReadJson (stringReader);
			}
		}

		static JObject ReadJson (Stream stream)
		{
			using (var streamReader = new StreamReader (stream)) {
				return ReadJson (streamReader);
			}
		}

		static JObject ReadJson (TextReader textReader)
		{
			using (var reader = new JsonTextReader (textReader)) {
				return (JObject)JToken.ReadFrom (reader);
			}
		}

		public static JObject GetDefaultSettingsFromResources (Type type)
		{
			return GetDefaultSettingsFromResources (type.Assembly);
		}

		public static JObject GetSettings (Type type, IEnumerable<string> configurationSections, string json)
		{
			if (!AnySections (configurationSections)) {
				return null;
			}

			JObject defaultSettings = GetDefaultSettingsFromResources (type);

			return GetSettings (configurationSections, json, defaultSettings);
		}

		public static JObject GetDefaultSettingsFromResources (Assembly assembly)
		{
			if (!VSWorkspaceJsonFileResourceExists (assembly)) {
				return null;
			}

			return ReadJson (assembly.GetManifestResourceStream (VSWorkspaceJsonFileName));
		}

		static bool VSWorkspaceJsonFileResourceExists (Assembly assembly)
		{
			return assembly
				.GetManifestResourceNames ()
				.Any (name => VSWorkspaceJsonFileName.Equals (name, StringComparison.OrdinalIgnoreCase));
		}
	}
}
