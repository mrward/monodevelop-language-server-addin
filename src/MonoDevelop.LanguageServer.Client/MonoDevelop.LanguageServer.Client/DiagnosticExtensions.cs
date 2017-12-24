//
// DiagnosticExtensions.cs
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

using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.LanguageServer.Client
{
	static class DiagnosticExtensions
	{
		public static Error CreateError (this Diagnostic diagnostic)
		{
			return new Error (
				GetErrorType (diagnostic.Severity),
				diagnostic.Code,
				diagnostic.Message,
				GetRegion (diagnostic.Range)
			);
		}

		static ErrorType GetErrorType (DiagnosticSeverity severity)
		{
			switch (severity) {
				case DiagnosticSeverity.Error:
					return ErrorType.Error;

				case DiagnosticSeverity.Warning:
				case DiagnosticSeverity.Hint: // No hint type so use warning for now.
				case DiagnosticSeverity.Information: // No info type so use warning for now.
					return ErrorType.Warning;

				default:
					return ErrorType.Unknown; // Same as returning ErrorType.Error
			}
		}

		static DocumentRegion GetRegion (Range range)
		{
			return new DocumentRegion (
				range.Start.Line + 1,
				range.Start.Character + 1,
				range.End.Line + 1,
				range.End.Character + 1
			);
		}
	}
}
