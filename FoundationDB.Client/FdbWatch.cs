﻿#region BSD License
/* Copyright (c) 2013-2020, Doxense SAS
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Client
{
	using System;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using Doxense.Diagnostics.Contracts;
	using FoundationDB.Client.Native;
	using JetBrains.Annotations;

	/// <summary>Watch that triggers when the watched key is changed in the database</summary>
	[DebuggerDisplay("Status={Future.Task.Status}, Key={Key}")]
	[PublicAPI]
	public sealed class FdbWatch : IDisposable
	{

		public FdbWatch(FdbFuture<Slice> future, Slice key)
		{
			Contract.Debug.Requires(future != null);
			this.Future = future;
			this.Key = key;
		}

		private readonly FdbFuture<Slice> Future;

		/// <summary>Key that is being watched</summary>
		public readonly Slice Key;

		/// <summary>Returns true if the watch is still active, or false if it fired or was cancelled</summary>
		public bool IsAlive => !this.Future.Task.IsCompleted;

		/// <summary>Task that will complete when the watch fires, or is cancelled. It will return the watched key, or an exception.</summary>
		public Task<Slice> Task => this.Future.Task;

		/// <summary>Returns an awaiter for the Watch</summary>
		public TaskAwaiter<Slice> GetAwaiter()
		{
			//note: this is to make "await" work directly on the FdbWatch instance, without needing to do "await watch.Task"

			if (this.Future.HasFlag(FdbFuture.Flags.DISPOSED))
			{
				throw ThrowHelper.ObjectDisposedException("Cannot await a watch that has already been disposed");
			}
			return this.Future.Task.GetAwaiter();
		}

		/// <summary>Cancel the watch. It will immediately stop monitoring the key. Has no effect if the watch has already fired</summary>
		public void Cancel()
		{
			this.Future.Cancel();
		}

		/// <summary>Dispose the resources allocated by the watch.</summary>
		public void Dispose()
		{
			this.Future.Dispose();
		}

		public override string ToString()
		{
			return "Watch(" + FdbKey.Dump(this.Key) + ")";
		}

	}

}
