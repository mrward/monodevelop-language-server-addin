//
// SolutionLanguageClientSessions.cs
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
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.LanguageServer.Client
{
	class SolutionLanguageClientSessions
	{
		Dictionary<string, Dictionary<string, LanguageClientSession>> sessions;

		public SolutionLanguageClientSessions ()
		{
			Clear ();
		}

		public void Clear ()
		{
			sessions = new Dictionary<string, Dictionary<string, LanguageClientSession>> (StringComparer.OrdinalIgnoreCase);
		}

		public bool TryGetSession (IContentType contentType, Project project, out LanguageClientSession session)
		{
			session = null;

			if (project?.ParentSolution == null) {
				return false;
			}

			var solutionSessions = GetSolutionSession (project);
			if (solutionSessions == null) {
				return false;
			}

			return solutionSessions.TryGetValue (contentType.TypeName, out session);
		}

		Dictionary<string, LanguageClientSession> GetSolutionSession (Project project)
		{
			return GetSolutionSession (project.ParentSolution.BaseDirectory);
		}

		Dictionary<string, LanguageClientSession> GetSolutionSession (FilePath rootPath)
		{
			Dictionary<string, LanguageClientSession> solutionSessions = null;

			if (sessions.TryGetValue (rootPath.ToString (), out solutionSessions)) {
				return solutionSessions;
			}

			return null;
		}

		public void AddSession (LanguageClientSession session)
		{
			var solutionSessions = GetSolutionSession (session.RootPath);
			if (solutionSessions == null) {
				solutionSessions = new Dictionary<string, LanguageClientSession> ();
				sessions [session.RootPath.ToString ()] = solutionSessions;
			}

			solutionSessions [session.Id] = session;
		}

		public IEnumerable<LanguageClientSession> GetAllSessions ()
		{
			foreach (var solutionSessions in sessions.Values) {
				foreach (var session in solutionSessions.Values) {
					yield return session;
				}
			}
		}

		public void RemoveSession (LanguageClientSession session)
		{
			var solutionSessions = GetSolutionSession (session.RootPath);
			if (solutionSessions != null) {
				solutionSessions.Remove (session.Id);
				if (solutionSessions.Keys.Count == 0) {
					sessions.Remove (session.RootPath);
				}
			}
		}
	}
}
