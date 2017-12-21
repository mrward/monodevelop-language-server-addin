//
// TplExtensions.cs
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
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading
{
	#pragma warning disable CS0436 // Type conflicts with imported type

	public static class TplExtensions
	{
		/// <summary>
		/// Each handler is executed before the next handler in the list is executed.
		/// Code is still not completely correct - should execute all handlers even if they
		/// throw an exception.
		/// 
		/// https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.threading.tplextensions.invokeasync
		/// </summary>
		public static async Task InvokeAsync<TEventArgs> (this AsyncEventHandler<TEventArgs> handlers, object sender, TEventArgs e)
			where TEventArgs : EventArgs
		{
			foreach (AsyncEventHandler<TEventArgs> handler in handlers.GetInvocationList ()) {
				await handler (sender, e);
			}
		}
	}

	#pragma warning restore CS0436 // Type conflicts with imported type
}
