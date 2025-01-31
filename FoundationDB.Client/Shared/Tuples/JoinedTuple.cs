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

#if !USE_SHARED_FRAMEWORK

namespace Doxense.Collections.Tuples
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using Doxense.Runtime.Converters;
	using Doxense.Diagnostics.Contracts;
	using JetBrains.Annotations;

	/// <summary>Tuple that represents the concatenation of two tuples</summary>
	[DebuggerDisplay("{ToString(),nq}")]
	[PublicAPI]
	public sealed class JoinedTuple : IVarTuple
	{
		// Uses cases: joining a 'subspace' tuple (customerId, 'Users', ) with a 'key' tuple (userId, 'Contacts', 123, )

		/// <summary>First tuple (first N items)</summary>
		public readonly IVarTuple Head;

		/// <summary>Second tuple (last M items)</summary>
		public readonly IVarTuple Tail;

		/// <summary>Offset at which the Tail tuple starts. Items are in Head tuple if index &lt; split. Items are in Tail tuple if index &gt;= split.</summary>
		private readonly int m_split;

		/// <summary>Total size of the tuple (sum of the size of the two inner tuples)</summary>
		private readonly int m_count;

		public JoinedTuple(IVarTuple head, IVarTuple tail)
		{
			Contract.NotNull(head);
			Contract.NotNull(tail);

			this.Head = head;
			this.Tail = tail;
			m_split = head.Count;
			m_count = m_split + tail.Count;
		}

		public override string ToString()
		{
			return STuple.Formatter.ToString(this);
		}

		public int Count => m_count;

		public object? this[int index]
		{
			get
			{
				int p = TupleHelpers.MapIndex(index, m_count);
				return p < m_split ? this.Head[p] : this.Tail[p - m_split];
			}
		}

		public IVarTuple this[int? fromIncluded, int? toExcluded]
		{
			get
			{
				int begin = fromIncluded.HasValue ? TupleHelpers.MapIndexBounded(fromIncluded.Value, m_count) : 0;
				int end = toExcluded.HasValue ? TupleHelpers.MapIndexBounded(toExcluded.Value, m_count) : m_count;

				if (end <= begin) return STuple.Empty;

				int p = this.Head.Count;
				if (begin >= p)
				{ // all selected items are in the tail
					return this.Tail[begin - p, end - p];
				}
				if (end <= p)
				{ // all selected items are in the head
					return this.Head[begin, end];
				}
				// selected items are both in head and tail
				return new JoinedTuple(this.Head[begin, null], this.Tail[null, end - p]);
			}
		}

#if USE_RANGE_API

		public object? this[Index index]
		{
			get
			{
				int p = TupleHelpers.MapIndex(index, m_count);
				return p < m_split ? this.Head[p] : this.Tail[p - m_split];
			}
		}

		public IVarTuple this[Range range]
		{
			get
			{
				int lenHead = this.Head.Count;
				int lenTail = this.Tail.Count;
				(int offset, int count) = range.GetOffsetAndLength(lenHead + lenTail);
				if (count == 0) return STuple.Empty;
				if (offset == 0)
				{
					if (count == lenHead + lenTail) return this;
					if (count == lenHead) return this.Head;
				}
				if (offset == lenHead && count == lenTail)
				{
					return this.Tail;
				}
				return TupleHelpers.Splice(this, range);
			}
		}

#endif

		[return: MaybeNull]
		public T Get<T>(int index)
		{
			index = TupleHelpers.MapIndex(index, m_count);
			return index < m_split ? this.Head.Get<T>(index) : this.Tail.Get<T>(index - m_split);
		}

		IVarTuple IVarTuple.Append<T>(T value)
		{
			return new LinkedTuple<T>(this, value);
		}

		public LinkedTuple<T> Append<T>(T value)
		{
			return new LinkedTuple<T>(this, value);
		}

		public IVarTuple Concat(IVarTuple tuple)
		{
			Contract.NotNull(tuple);

			int n1 = tuple.Count;
			if (n1 == 0) return this;

			int n2 = this.Count;

			if (n1 + n2 >= 10)
			{ // it's getting big, merge to a new List tuple
				return STuple.Concat(this.Head, this.Tail, tuple);
			}
			// REVIEW: should we always concat with the tail?
			return STuple.Concat(this.Head, this.Tail.Concat(tuple));
		}

		public void CopyTo(object?[] array, int offset)
		{
			this.Head.CopyTo(array, offset);
			this.Tail.CopyTo(array, offset + m_split);
		}

		public IEnumerator<object?> GetEnumerator()
		{
			foreach (var item in this.Head)
			{
				yield return item;
			}
			foreach (var item in this.Tail)
			{
				yield return item;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public override bool Equals(object? obj)
		{
			return obj != null && ((IStructuralEquatable)this).Equals(obj, SimilarValueComparer.Default);
		}

		public bool Equals(IVarTuple? other)
		{
			return !object.ReferenceEquals(other, null) && ((IStructuralEquatable)this).Equals(other, SimilarValueComparer.Default);
		}

		public override int GetHashCode()
		{
			return ((IStructuralEquatable)this).GetHashCode(SimilarValueComparer.Default);
		}

		bool System.Collections.IStructuralEquatable.Equals(object? other, System.Collections.IEqualityComparer comparer)
		{
			if (object.ReferenceEquals(this, other)) return true;
			if (other == null) return false;

			var tuple = other as IVarTuple;
			if (!object.ReferenceEquals(tuple, null))
			{
				if (tuple.Count != m_count) return false;

				using (var iter = tuple.GetEnumerator())
				{
					foreach (var item in this.Head)
					{
						if (!iter.MoveNext() || !comparer.Equals(item, iter.Current)) return false;
					}
					foreach (var item in this.Tail)
					{
						if (!iter.MoveNext() || !comparer.Equals(item, iter.Current)) return false;
					}
					return !iter.MoveNext();
				}
			}

			return false;
		}

		int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer)
		{
			int h = this.Head.GetHashCode(comparer);
			// we can't combine with the hashcode of the tail because Combine(H(head), H(tail)) != Combine(H(this[0]), H(this[1]), ... H(this[N-1]))
			foreach (var item in this.Tail)
			{
				h = HashCodes.Combine(h, comparer.GetHashCode(item));
			}
			return h;
		}
	}

}

#endif
