//
// JsonRpcExtensions.cs
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace MonoDevelop.LanguageServer.Client
{
	static class JsonRpcExtensions
	{
		/// <summary>
		/// Returns true if the invoke completed successfully.
		/// </summary>
		public static Task<bool> InvokeAsyncWithTimeout (this JsonRpc jsonRpc, string targetName, int timeout)
		{
			Task task = jsonRpc.InvokeAsync (targetName);
			return WaitWithTimeout (task, timeout);
		}

		static async Task<bool> WaitWithTimeout (Task task, int timeout)
		{
			Task result = await Task.WhenAny (task, Task.Delay (timeout));
			return result == task;
		}

		public static Task<TResult> InvokeWithParameterObjectAsync<TArgument, TResult> (
			this JsonRpc jsonRpc,
			LspRequest<TArgument, TResult> request,
			TArgument argument,
			CancellationToken cancellationToken = default (CancellationToken))
		{
			return jsonRpc.InvokeWithParameterObjectAsync<TResult> (request.Name, argument, cancellationToken);
		}

		public static Task NotifyWithParameterObjectAsync<TArgument> (
			this JsonRpc jsonRpc,
			LspNotification<TArgument> notification,
			TArgument argument)
		{
			return jsonRpc.NotifyWithParameterObjectAsync (notification.Name, argument);
		}
	}
}
