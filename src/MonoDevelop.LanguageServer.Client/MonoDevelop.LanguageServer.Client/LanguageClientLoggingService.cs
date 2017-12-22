//
// LanguageClientLoggingService.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.LanguageServer.Client
{
	public static class LanguageClientLoggingService
	{
		public static void Log (string message)
		{
			LanguageClientOutputPad.WriteText (message);
		}

		public static void Log (string format, object arg0)
		{
			string message = string.Format (format, arg0);
			Log (message);
		}

		public static void Log (string format, object arg0, object arg1)
		{
			string message = string.Format (format, arg0, arg1);
			Log (message);
		}

		public static void LogError (string message)
		{
			LanguageClientOutputPad.WriteError (message);
		}

		public static void LogError (string message, Exception ex)
		{
			LoggingService.LogError (message, ex);

			LogError (message + Environment.NewLine + ex);
		}

		public static void LogError (Exception ex, string format, object arg0)
		{
			string message = string.Format (format, arg0);
			LogError (message, ex);
		}
	}
}
