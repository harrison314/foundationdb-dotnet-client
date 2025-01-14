﻿#region Copyright (c) 2013-2022, Doxense SAS. All rights reserved.
// See License.MD for license information
#endregion

#if !USE_SHARED_FRAMEWORK

#nullable enable

namespace Doxense.Diagnostics.Contracts
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using SDC = System.Diagnostics.Contracts;
	using SRC = System.Runtime.CompilerServices;
	using JetBrains.Annotations;

	internal static class ContractMessages
	{

		public const string ValueCannotBeNull = "Value cannot be null.";
		public const string StringCannotBeEmpty = "String cannot be empty.";
		public const string StringCannotBeWhiteSpaces = "String cannot contain only whitespaces.";
		public const string CollectionCannotBeEmpty = "Collection cannot be empty.";
		public const string BufferCannotBeNull = "Buffer cannot be null.";
		public const string BufferCannotBeEmpty = "Buffer cannot be empty.";
		public const string PositiveNumberRequired = "Positive number required.";
		public const string PowerOfTwoRequired = "Power of two number required.";
		public const string AboveZeroNumberRequired = "Non-Zero Positive number required.";
		public const string ValueIsTooSmall = "The specified value is too small.";
		public const string ValueIsTooBig = "The specified value is too big.";
		public const string ValueIsForbidden = "The specified value is not allowed.";
		public const string ValueIsExpected = "The specified value is not the expected value.";
		public const string ValueMustBeBetween = "The specified value was outside the specified range.";
		public const string ValueMustBeMultiple = "The specified value must be a multiple of another value.";
		public const string NonNegativeNumberRequired = "Non-negative number required.";
		public const string OffsetMustBeWithinBuffer = "Offset and length were out of bounds for the buffer or count is greater than the number of elements from index to the end of the buffer.";

		public const string ConditionNotNull = "{0} != null";
		public const string ConditionNotEmptyLength = "{0}.Length > 0";
		public const string ConditionNotWhiteSpace = "{0}.All(c => !char.IsWhiteSpace(c))";
		public const string ConditionNotEmptyCount = "{0}.Count > 0";
		public const string ConditionArgPositive = "{0} >= 0";
		public const string ConditionArgNotEqualTo = "{0} != x";
		public const string ConditionArgEqualTo = "{0} == x";
		public const string ConditionArgGreaterThan = "{0} > x";
		public const string ConditionArgGreaterThanZero = "{0} > 0";
		public const string ConditionArgGreaterOrEqual = "{0} >= x";
		public const string ConditionArgGreaterOrEqualZero = "{0} >= 0";
		public const string ConditionArgMultiple = "{0} % x == 0";
		public const string ConditionArgLessThan = "{0} < x";
		public const string ConditionArgLessThanOrEqual = "{0} <= x";
		public const string ConditionArgBetween = "min <= {0} <= max";
		public const string ConditionArgBufferOverflow = "(buffer.Length - offset) < count";
	}

	/// <summary>Classe helper pour la vérification de pré-requis, invariants, assertions, ...</summary>
	[DebuggerNonUserCode]
	[PublicAPI]
	internal static partial class Contract
	{

		private static readonly ConstructorInfo? s_constructorNUnitException = GetAssertionExceptionCtor();

		public static bool IsUnitTesting { get; set; }

		private static ConstructorInfo? GetAssertionExceptionCtor()
		{
			// détermine si on est lancé depuis des tests unitaires (pour désactiver les breakpoints et autres opérations intrusives qui vont parasiter les tests)

			var nUnitAssert = Type.GetType("NUnit.Framework.AssertionException,nunit.framework");
			if (nUnitAssert != null)
			{
				// on convertit les échecs "soft" en échec d'assertion NUnit
				IsUnitTesting = true;
				return nUnitAssert.GetConstructor(new [] { typeof (string) });
			}
			return null;
		}

		private static Exception? MapToNUnitAssertion(string message)
		{
			return (Exception?) s_constructorNUnitException?.Invoke(new object[] { message }); // => new NUnit.Framework.AssertionException(...)
		}

		#region RUNTIME checks...

		/// <summary>Vérifie qu'une pré-condition est vrai, lors de l'entrée dans une méthode</summary>
		/// <param name="condition">Condition qui ne doit jamais être fausse</param>
		/// <param name="userMessage">Message décrivant l'erreur (optionnel)</param>
		/// <param name="conditionText">Texte de la condition (optionnel, injecté par le compilateur)</param>
		/// <remarks>Ne fait rien si la condition est vrai. Sinon déclenche une ContractException, après avoir essayé de breakpointer le debugger</remarks>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Requires(
			[AssertionCondition(AssertionConditionType.IS_TRUE)]
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)]
#endif
			bool condition,
			string? userMessage = null,
			[CallerArgumentExpression("condition")] string? conditionText = null
		)
		{
			if (!condition) throw RaiseContractFailure(SDC.ContractFailureKind.Precondition, userMessage, conditionText);
		}

		/// <summary>Vérifie qu'une condition est toujours vrai, dans le body dans une méthode</summary>
		/// <param name="condition">Condition qui ne doit jamais être fausse</param>
		/// <param name="userMessage">Message décrivant l'erreur (optionnel)</param>
		/// <param name="conditionText">Texte de la condition (optionnel, injecté par le compilateur)</param>
		/// <remarks>Ne fait rien si la condition est vrai. Sinon déclenche une ContractException, après avoir essayé de breakpointer le debugger</remarks>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Assert(
			[AssertionCondition(AssertionConditionType.IS_TRUE)]
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)]
#endif
			bool condition,
			string? userMessage = null,
			[CallerArgumentExpression("condition")] string? conditionText = null)
		{
			if (!condition) throw RaiseContractFailure(SDC.ContractFailureKind.Assert, userMessage, conditionText);
		}

		/// <summary>Vérifie qu'une condition est toujours vrai, lors de la sortie d'une méthode</summary>
		/// <param name="condition">Condition qui ne doit jamais être fausse</param>
		/// <param name="userMessage">Message décrivant l'erreur (optionnel)</param>
		/// <param name="conditionText">Texte de la condition (optionnel, injecté par le compilateur)</param>
		/// <remarks>Ne fait rien si la condition est vrai. Sinon déclenche une ContractException, après avoir essayé de breakpointer le debugger</remarks>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Ensures(
			[AssertionCondition(AssertionConditionType.IS_TRUE)]
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)]
#endif
			bool condition,
			string? userMessage = null,
			[CallerArgumentExpression("condition")] string? conditionText = null
		)
		{
			if (!condition) throw RaiseContractFailure(SDC.ContractFailureKind.Postcondition, userMessage, conditionText);
		}

		/// <summary>Vérifie qu'une condition est toujours vrai pendant toute la vie d'une instance</summary>
		/// <param name="condition">Condition qui ne doit jamais être fausse</param>
		/// <param name="userMessage">Message décrivant l'erreur (optionnel)</param>
		/// <param name="conditionText">Texte de la condition (optionnel, injecté par le compilateur)</param>
		/// <remarks>Ne fait rien si la condition est vrai. Sinon déclenche une ContractException, après avoir essayé de breakpointer le debugger</remarks>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Invariant(
			[AssertionCondition(AssertionConditionType.IS_TRUE)]
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)]
#endif
			bool condition,
			string? userMessage = null,
			[CallerArgumentExpression("condition")] string? conditionText = null
		)
		{
			if (!condition) throw RaiseContractFailure(SDC.ContractFailureKind.Invariant, userMessage, conditionText);
		}

		#region Contract.NotNull

		/// <summary>The specified instance must not be null (assert: value != null)</summary>
		/// <exception cref="ArgumentNullException">if <paramref name="value"/> is null</exception>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNull<TValue>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL), NoEnumeration] TValue value,
			[InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
			where TValue : class
		{
			if (value == null) throw FailArgumentNull(paramName!);
		}

		/// <summary>The specified instance must not be null (assert: value != null)</summary>
		/// <exception cref="ArgumentNullException">if <paramref name="value"/> is null</exception>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNull(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL), NoEnumeration] string value,
			[InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value == null) throw FailArgumentNull(paramName!);
		}

		/// <summary>The specified instance must not be null (assert: value != null)</summary>
		/// <exception cref="ArgumentNullException">if <paramref name="value"/> is null</exception>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNull<TValue>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL), NoEnumeration] TValue value,
			[InvokerParameterName] string paramName,
			string message)
			where TValue : class
		{
			if (value == null) throw FailArgumentNull(paramName, message);
		}

		/// <summary>The specified instance must not be null (assert: value != null)</summary>
		/// <remarks>This methods allow structs (that can never be null)</remarks>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullAllowStructs<TValue>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL), NoEnumeration] TValue value,
			[InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value == null) throw FailArgumentNull(paramName!);
		}

		/// <summary>The specified instance must not be null (assert: value != null)</summary>
		/// <remarks>This methods allow structs (that can never be null)</remarks>
		/// <exception cref="ArgumentNullException">if <paramref name="value"/> is null</exception>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullAllowStructs<TValue>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL), NoEnumeration] TValue value,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (value == null) throw FailArgumentNull(paramName, message);
		}

		/// <summary>The specified pointer must not be null (assert: pointer != null)</summary>
		/// <exception cref="ArgumentNullException">if <paramref name="pointer"/> is null</exception>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void PointerNotNull(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] void* pointer,
			[InvokerParameterName, CallerArgumentExpression("pointer")] string? paramName = null)
		{
			if (pointer == null) throw FailArgumentNull(paramName!);
		}

		/// <summary>The specified pointer must not be null (assert: pointer != null)</summary>
		/// <exception cref="ArgumentNullException">if <paramref name="pointer"/> is null</exception>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void PointerNotNull(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] void* pointer,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (pointer == null) throw FailArgumentNull(paramName, message);
		}

		/// <summary>The specified value cannot be null (assert: value != null)</summary>
		/// <returns>Passed value, or throws an exception if it was null</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="value"/> is null</exception>
		/// <remarks>This method is intended for use in single-line property setters</remarks>
		/// <example><code>
		/// public string FooThatIsNeverNull
		/// {
		///     get => return m_foo;
		///     set => m_foo = Contract.ValueNotNull(value);
		/// }
		/// </code> </example>
		[Pure, AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ValueNotNull<T>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL), NoEnumeration] T value
		)
			where T : class
		{
			return value ?? throw FailArgumentNull(nameof(value));
		}

		/// <summary>The specified value cannot be null (assert: value != null)</summary>
		/// <returns>Passed value, or throws an exception if it was null</returns>
		/// <exception cref="ArgumentNullException">if <paramref name="value"/> is null</exception>
		/// <remarks>This method is intended for use in single-line property setters</remarks>
		/// <example><code>
		/// private string m_fooThatIsNeverNull;
		/// public string Foo
		/// {
		///     get => return m_fooThatIsNeverNull;
		///     set => m_fooThatIsNeverNull = Contract.ValueNotNull(value, "Foo cannot be set to null");
		/// }
		/// </code> </example>
		[Pure, AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ValueNotNull<T>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL), NoEnumeration] T value,
			string message
		)
		{
			return value ?? throw FailArgumentNull(nameof(value), message);
		}

		#endregion

		#region Contract.NotNullOrEmpty

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailStringNullOrEmpty(string? value, string? paramName, string? message = null)
		{
			return value == null
				? ReportFailure(typeof(ArgumentNullException), ContractMessages.ValueCannotBeNull, message, paramName, ContractMessages.ConditionNotNull)
				: ReportFailure(typeof(ArgumentException), ContractMessages.StringCannotBeEmpty, message, paramName, ContractMessages.ConditionNotEmptyLength);
		}

		/// <summary>The specified string must not be null or empty (assert: value != null &amp;&amp; value.Length != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] string value,
			[InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null
		)
		{
			if (string.IsNullOrEmpty(value)) throw FailStringNullOrEmpty(value, paramName, null);
		}

		/// <summary>The specified string must not be null or empty (assert: value != null &amp;&amp; value.Length != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] string value,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (string.IsNullOrEmpty(value)) throw FailStringNullOrEmpty(value, paramName, message);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailStringNullOrWhiteSpace(string? value, string? paramName, string? message = null)
		{
			return value == null ? ReportFailure(typeof(ArgumentNullException), ContractMessages.ValueCannotBeNull, message, paramName, ContractMessages.ConditionNotNull)
				: value.Length == 0 ? ReportFailure(typeof(ArgumentException), ContractMessages.StringCannotBeEmpty, message, paramName, ContractMessages.ConditionNotEmptyLength)
				: ReportFailure(typeof(ArgumentException), ContractMessages.StringCannotBeWhiteSpaces, message, paramName, ContractMessages.ConditionNotWhiteSpace);
		}

		/// <summary>The specified string must not be null, empty or contain only whitespaces (assert: value != null &amp;&amp; value.Length != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrWhiteSpace(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.DisallowNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] string value,
			[InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null
		)
		{
			if (string.IsNullOrWhiteSpace(value)) throw FailStringNullOrWhiteSpace(value, paramName, null);
		}

		/// <summary>The specified string must not be null, empty or contain only whitespaces (assert: value != null &amp;&amp; value.Length != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrWhiteSpace(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.DisallowNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] string value,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (string.IsNullOrWhiteSpace(value)) throw FailStringNullOrWhiteSpace(value, paramName, message);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArrayNullOrEmpty(object? collection, string? paramName, string? message = null)
		{
			return collection == null
				? ReportFailure(typeof(ArgumentNullException), ContractMessages.ValueCannotBeNull, message, paramName, ContractMessages.ConditionNotNull)
				: ReportFailure(typeof(ArgumentException), ContractMessages.CollectionCannotBeEmpty, message, paramName, ContractMessages.ConditionNotEmptyCount);
		}

		/// <summary>The specified array must not be null or empty (assert: value != null &amp;&amp; value.Count != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty<T>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] T[] value,
			[InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null
		)
		{
			if (value == null || value.Length == 0) throw FailArrayNullOrEmpty(value, paramName, null);
		}

		/// <summary>The specified array must not be null or empty (assert: value != null &amp;&amp; value.Count != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty<T>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] T[] value,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (value == null || value.Length == 0) throw FailArrayNullOrEmpty(value, paramName, message);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailCollectionNullOrEmpty(object? collection, string paramName, string? message = null)
		{
			return collection == null
				? ReportFailure(typeof(ArgumentNullException), ContractMessages.ValueCannotBeNull, message, paramName, ContractMessages.ConditionNotNull)
				: ReportFailure(typeof(ArgumentException), ContractMessages.CollectionCannotBeEmpty, message, paramName, ContractMessages.ConditionNotEmptyCount);
		}

		/// <summary>The specified collection must not be null or empty (assert: value != null &amp;&amp; value.Count != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty<T>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] ICollection<T> value,
			[InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null
		)
		{
			if (value == null || value.Count == 0) throw FailCollectionNullOrEmpty(value, paramName!, null);
		}

		/// <summary>The specified collection must not be null or empty (assert: value != null &amp;&amp; value.Count != 0)</summary>
		[AssertionMethod, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty<T>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.AllowNull]
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] ICollection<T> value,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (value == null || value.Count == 0) throw FailCollectionNullOrEmpty(value, paramName, message);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailBufferNull(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentNullException), ContractMessages.BufferCannotBeNull, message, paramName, ContractMessages.ConditionNotNull);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailBufferNullOrEmpty(object? array, string paramName, string? message = null)
		{
			return array == null
				? ReportFailure(typeof(ArgumentNullException), ContractMessages.BufferCannotBeNull, message, paramName, ContractMessages.ConditionNotNull)
				: ReportFailure(typeof(ArgumentException), ContractMessages.BufferCannotBeEmpty, message, paramName, ContractMessages.ConditionNotEmptyCount);
		}

		/// <summary>The specified buffer must not be null or empty (assert: buffer.Array != null &amp;&amp; buffer.Count != 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty(
			Slice buffer,
			[InvokerParameterName, CallerArgumentExpression("buffer")] string? paramName = null
		)
		{
			if (buffer.Array == null | buffer.Count == 0) throw FailBufferNullOrEmpty(buffer.Array, paramName!, null);
		}

		/// <summary>The specified buffer must not be null or empty (assert: buffer.Array != null &amp;&amp; buffer.Count != 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty(
			Slice buffer,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (buffer.Array == null | buffer.Count == 0) throw FailBufferNullOrEmpty(buffer.Array, paramName, message);
		}

		/// <summary>The specified buffer must not be null or empty (assert: buffer.Array != null &amp;&amp; buffer.Count != 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty<T>(
			ArraySegment<T> buffer,
			[InvokerParameterName, CallerArgumentExpression("buffer")] string? paramName = null)
		{
			if (buffer.Array == null | buffer.Count == 0) throw FailBufferNullOrEmpty(buffer.Array, paramName!, null);
		}

		/// <summary>The specified buffer must not be null or empty (assert: buffer.Array != null &amp;&amp; buffer.Count != 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotNullOrEmpty<T>(
			ArraySegment<T> buffer,
			[InvokerParameterName] string paramName,
			string message)
		{
			if (buffer.Array == null | buffer.Count == 0) throw FailBufferNullOrEmpty(buffer.Array, paramName, message);
		}

		#endregion

		#region Contract.Positive, LessThan[OrEqual], GreaterThen[OrEqual], EqualTo, Between, ...

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotPositive(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.PositiveNumberRequired, message, paramName, ContractMessages.ConditionArgPositive);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotNonNegative(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.NonNegativeNumberRequired, message, paramName, ContractMessages.ConditionArgPositive);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotPowerOfTwo(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.PowerOfTwoRequired, message, paramName, ContractMessages.ConditionArgPositive);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentForbidden<T>(string paramName, T forbidden, string? message = null)
		{
			//TODO: need support for two format arguments for conditionTxt !
			return ReportFailure(typeof(ArgumentException), ContractMessages.ValueIsForbidden, message, paramName, ContractMessages.ConditionArgNotEqualTo);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentExpected<T>(string paramName, T expected, string? message = null)
		{
			//TODO: need support for two format arguments for conditionTxt !
			return ReportFailure(typeof(ArgumentException), ContractMessages.ValueIsExpected, message, paramName, ContractMessages.ConditionArgEqualTo);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotGreaterThan(string paramName, bool zero, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), zero ? ContractMessages.AboveZeroNumberRequired : ContractMessages.ValueIsTooSmall, message, paramName, zero ? ContractMessages.ConditionArgGreaterThanZero : ContractMessages.ConditionArgGreaterThan);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotGreaterOrEqual(string paramName, bool thresholdIsZero, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), thresholdIsZero ? ContractMessages.PositiveNumberRequired : ContractMessages.ValueIsTooSmall, message, paramName, thresholdIsZero ? ContractMessages.ConditionArgGreaterOrEqualZero : ContractMessages.ConditionArgGreaterOrEqual);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotLessThan(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.ValueIsTooBig, message, paramName, ContractMessages.ConditionArgLessThan);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotLessOrEqual(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.ValueIsTooBig, message, paramName, ContractMessages.ConditionArgLessThanOrEqual);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentOutOfBounds(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.ValueMustBeBetween, message, paramName, ContractMessages.ConditionArgBetween);
		}

		#region Positive...

		/// <summary>The specified value must not be a negative number (assert: value >= 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Positive(int value, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < 0) throw FailArgumentNotPositive(paramName!, null);
		}

		/// <summary>The specified value must not be a negative number (assert: value >= 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Positive(int value, [InvokerParameterName] string paramName, string message)
		{
			if (value < 0) throw FailArgumentNotPositive(paramName, message);
		}

		/// <summary>The specified value must not be a negative number (assert: value >= 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Positive(long value, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < 0) throw FailArgumentNotPositive(paramName!, null);
		}

		/// <summary>The specified value must not be a negative number (assert: value >= 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Positive(long value, [InvokerParameterName] string paramName, string message)
		{
			if (value < 0) throw FailArgumentNotPositive(paramName, message);
		}

		/// <summary>The specified value must not be a negative number (assert: value >= 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Positive(double value, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (Math.Sign(value) != 1) throw FailArgumentNotPositive(paramName!, null);
		}

		/// <summary>The specified value must not be a negative number (assert: value >= 0)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Positive(double value, [InvokerParameterName] string paramName, string message)
		{
			if (Math.Sign(value) != 1) throw FailArgumentNotPositive(paramName, message);
		}

		#endregion

		#region PowerOfTwo...

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(int value, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < 0 || unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName!, null);
		}

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(int value, [InvokerParameterName] string paramName, string message)
		{
			if (value < 0 || unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName, message);
		}

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(uint value, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName!, null);
		}

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(uint value, [InvokerParameterName] string paramName, string message)
		{
			if (unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName, message);
		}

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(long value, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < 0 || unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName!, null);
		}

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(long value, [InvokerParameterName] string paramName, string message)
		{
			if (value < 0 || unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName, message);
		}

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(ulong value, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName!, null);
		}

		/// <summary>The specified value must be a power of two (assert: NextPowerOfTwo(value) == value)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void PowerOfTwo(ulong value, [InvokerParameterName] string paramName, string message)
		{
			if (unchecked((value & (value - 1)) != 0)) throw FailArgumentNotPowerOfTwo(paramName, message);
		}

		#endregion

		#region EqualTo...

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(int value, int expected, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value != expected) throw FailArgumentExpected(paramName!, expected, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(int value, int expected, [InvokerParameterName] string paramName, string message)
		{
			if (value != expected) throw FailArgumentExpected(paramName, expected, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(long value, long expected, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value != expected) throw FailArgumentExpected(paramName!, expected, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(long value, long expected, [InvokerParameterName] string paramName, string message)
		{
			if (value != expected) throw FailArgumentExpected(paramName, expected, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(uint value, uint expected, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value != expected) throw FailArgumentExpected(paramName!, expected, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(uint value, uint expected, [InvokerParameterName] string paramName, string message)
		{
			if (value != expected) throw FailArgumentExpected(paramName, expected, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(ulong value, ulong expected, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value != expected) throw FailArgumentExpected(paramName!, expected, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(ulong value, ulong expected, [InvokerParameterName] string paramName, string message)
		{
			if (value != expected) throw FailArgumentExpected(paramName, expected, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(string? value, string? expected, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value != expected) throw FailArgumentExpected(paramName!, expected, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo(string? value, string? expected, [InvokerParameterName] string paramName, string message)
		{
			if (value != expected) throw FailArgumentExpected(paramName, expected, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo<T>(T value, T expected, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
			where T : struct, IEquatable<T>
		{
			if (!value.Equals(expected)) throw FailArgumentExpected(paramName!, expected, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EqualTo<T>(T value, T expected, [InvokerParameterName] string paramName, string message)
			where T : struct, IEquatable<T>
		{
			if (!value.Equals(expected)) throw FailArgumentExpected(paramName, expected, message);
		}

		#endregion

		#region NotEqualTo...

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(int value, int forbidden, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName!, forbidden, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(int value, int forbidden, [InvokerParameterName] string paramName, string message)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName, forbidden, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(long value, long forbidden, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName!, forbidden, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(long value, long forbidden, [InvokerParameterName] string paramName, string message)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName, forbidden, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(uint value, uint forbidden, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName!, forbidden, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(uint value, uint forbidden, [InvokerParameterName] string paramName, string message)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName, forbidden, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(ulong value, ulong forbidden, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName!, forbidden, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(ulong value, ulong forbidden, [InvokerParameterName] string paramName, string message)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName, forbidden, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(string value, string forbidden, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName!, forbidden, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo(string value, string forbidden, [InvokerParameterName] string paramName, string message)
		{
			if (value == forbidden) throw FailArgumentForbidden(paramName, forbidden, message);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo<T>(T value, T forbidden, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
			where T : struct, IEquatable<T>
		{
			if (value.Equals(forbidden)) throw FailArgumentForbidden(paramName!, forbidden, null);
		}

		/// <summary>The specified value must not equal to the specified constant (assert: value != forbidden)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void NotEqualTo<T>(T value, T forbidden, [InvokerParameterName] string paramName, string message)
			where T : struct, IEquatable<T>
		{
			if (value.Equals(forbidden)) throw FailArgumentForbidden(paramName, forbidden, message);
		}

		#endregion

		#region GreaterThan...

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(int value, int threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName!, threshold == 0, null);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(int value, int threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName, threshold == 0, message);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(uint value, uint threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName!, threshold == 0, null);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(uint value, uint threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName, threshold == 0, message);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(long value, long threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName!, threshold == 0, null);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(long value, long threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName, threshold == 0, message);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(ulong value, ulong threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName!, threshold == 0, null);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(ulong value, ulong threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName, threshold == 0, message);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(float value, float threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName!, threshold == 0.0f, null);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(float value, float threshold, [InvokerParameterName] string paramName, string message)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName, threshold == 0.0f, message);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(double value, double threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName!, threshold == 0.0d, null);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan(double value, double threshold, [InvokerParameterName] string paramName, string message)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value <= threshold) throw FailArgumentNotGreaterThan(paramName, threshold == 0.0d, message);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan<T>(T value, T threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
			where T: struct, IComparable<T>
		{
			if (value.CompareTo(threshold) <= 0) throw FailArgumentNotGreaterThan(paramName!, threshold.CompareTo(default(T)) == 0, null);
		}

		/// <summary>The specified value must not less than or equal to the specified lower bound (assert: value > threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterThan<T>(T value, T threshold, [InvokerParameterName] string paramName, string message)
			where T: struct, IComparable<T>
		{
			if (value.CompareTo(threshold) <= 0) throw FailArgumentNotGreaterThan(paramName, threshold.CompareTo(default(T)) == 0, message);
		}

		#endregion
		
		#region GreaterOrEqual...

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(int value, int threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName!, threshold == 0, null);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(int value, int threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName, threshold == 0, message);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(uint value, uint threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName!, false, null);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(uint value, uint threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName, false, message);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(long value, long threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName!, threshold == 0, null);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(long value, long threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName, threshold == 0, message);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(ulong value, ulong threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName!, false, null);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(ulong value, ulong threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName, false, message);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(float value, float threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName!, threshold == 0.0f, null);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(float value, float threshold, [InvokerParameterName] string paramName, string message)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName, threshold == 0.0f, message);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(double value, double threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName!, threshold == 0.0d, null);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual(double value, double threshold, [InvokerParameterName] string paramName, string message)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value < threshold) throw FailArgumentNotGreaterOrEqual(paramName, threshold == 0.0d, message);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual<T>(T value, T threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
			where T : struct, IComparable<T>
		{
			if (value.CompareTo(threshold) < 0) throw FailArgumentNotGreaterOrEqual(paramName!, threshold.CompareTo(default(T)) == 0, null);
		}

		/// <summary>The specified value must not less than the specified lower bound (assert: value >= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void GreaterOrEqual<T>(T value, T threshold, [InvokerParameterName] string paramName, string message)
			where T : struct, IComparable<T>
		{
			if (value.CompareTo(threshold) < 0) throw FailArgumentNotGreaterOrEqual(paramName, threshold.CompareTo(default(T)) == 0, message);
		}

		#endregion

		#region LessThan...

		/// <summary>The specified value must not greater than or equal to the specified upper bound</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(int value, int threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName!, null);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(int value, int threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName, message);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(uint value, uint threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName!, null);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(uint value, uint threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName, message);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(long value, long threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName!, null);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(long value, long threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName, message);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(ulong value, ulong threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName!, null);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(ulong value, ulong threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName, message);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(float value, float threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName!, null);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(float value, float threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName, message);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(double value, double threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName!, null);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan(double value, double threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value >= threshold) throw FailArgumentNotLessThan(paramName, message);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan<T>(T value, T threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
			where T : struct, IComparable<T>
		{
			if (value.CompareTo(threshold) >= 0) throw FailArgumentNotLessThan(paramName!, null);
		}

		/// <summary>The specified value must not greater than or equal to the specified upper bound (assert: value &lt; threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessThan<T>(T value, T threshold, [InvokerParameterName] string paramName, string message)
			where T : struct, IComparable<T>
		{
			if (value.CompareTo(threshold) >= 0) throw FailArgumentNotLessThan(paramName, message);
		}

		#endregion

		#region LessOrEqual...

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(int value, int threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName!, null);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(int value, int threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName, message);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(uint value, uint threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName!, null);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(uint value, uint threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName, message);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(long value, long threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName!, null);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(long value, long threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName, message);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(ulong value, ulong threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName!, null);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(ulong value, ulong threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName, message);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(float value, float threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName!, null);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(float value, float threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName, message);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(double value, double threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName!, null);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual(double value, double threshold, [InvokerParameterName] string paramName, string message)
		{
			if (value > threshold) throw FailArgumentNotLessOrEqual(paramName, message);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual<T>(T value, T threshold, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
			where T : struct, IComparable<T>
		{
			if (value.CompareTo(threshold) > 0) throw FailArgumentNotLessOrEqual(paramName!, null);
		}

		/// <summary>The specified value must not greater than the specified upper bound (assert: value &lt;= threshold)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LessOrEqual<T>(T value, T threshold, [InvokerParameterName] string paramName, string message)
			where T : struct, IComparable<T>
		{
			if (value.CompareTo(threshold) > 0) throw FailArgumentNotLessOrEqual(paramName, message);
		}

		#endregion

		#region Between...

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(int value, int minimumInclusive, int maximumInclusive, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName!, null);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(int value, int minimumInclusive, int maximumInclusive, [InvokerParameterName] string paramName, string message)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName, message);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(uint value, uint minimumInclusive, uint maximumInclusive, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName!, null);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(uint value, uint minimumInclusive, uint maximumInclusive, [InvokerParameterName] string paramName, string message)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName, message);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(long value, long minimumInclusive, long maximumInclusive, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName!, null);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(long value, long minimumInclusive, long maximumInclusive, [InvokerParameterName] string paramName, string message)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName, message);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(ulong value, ulong minimumInclusive, ulong maximumInclusive, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName!, null);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(ulong value, ulong minimumInclusive, ulong maximumInclusive, [InvokerParameterName] string paramName, string message)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName, message);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(float value, float minimumInclusive, float maximumInclusive, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName!, null);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(float value, float minimumInclusive, float maximumInclusive, [InvokerParameterName] string paramName, string message)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName, message);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(double value, double minimumInclusive, double maximumInclusive, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName!, null);
		}

		/// <summary>The specified value must not be outside of the specified bounds (assert: min &lt;= value &lt;= max)</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Between(double value, double minimumInclusive, double maximumInclusive, [InvokerParameterName] string paramName, string message)
		{
			if (value < minimumInclusive || value > maximumInclusive) throw FailArgumentOutOfBounds(paramName, message);
		}

		#endregion

		#region Multiple...

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNotMultiple(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.ValueMustBeMultiple, message, paramName, ContractMessages.ConditionArgMultiple);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(int value, int multiple, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName!, null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(int value, int multiple, [InvokerParameterName] string paramName, string message)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName, message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(uint value, uint multiple, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName!, null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(uint value, uint multiple, [InvokerParameterName] string paramName, string message)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName, message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(long value, long multiple, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName!, null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(long value, long multiple, [InvokerParameterName] string paramName, string message)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName, message);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(ulong value, ulong multiple, [InvokerParameterName, CallerArgumentExpression("value")] string? paramName = null)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName!, null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiple(ulong value, ulong multiple, [InvokerParameterName] string paramName, string message)
		{
			if (value % multiple != 0) throw FailArgumentNotMultiple(paramName, message);
		}

		#endregion

		#endregion

		#region Contract.DoesNotOverflow

		/// <summary>Vérifie qu'une couple index/count ne débord pas d'un buffer, et qu'il n'est pas null</summary>
		/// <param name="buffer">Buffer (qui ne doit pas être null)</param>
		/// <param name="index">Index (qui ne doit pas être négatif)</param>
		/// <param name="count">Taille (qui ne doit pas être négative)</param>
		/// <param name="message"></param>
		[AssertionMethod]
		public static void DoesNotOverflow(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)]
			string buffer,
			int index,
			int count,
			string? message = null)
		{
			if (buffer == null) throw FailArgumentNull("buffer", message);
			if (index < 0 || count < 0) throw FailArgumentNotNonNegative(index < 0 ? "index" : "count", message);
			if ((buffer.Length - index) < count) throw FailBufferTooSmall("count", message);
		}

		/// <summary>Vérifie qu'une couple index/count ne débord pas d'un buffer, et qu'il n'est pas null</summary>
		/// <param name="bufferLength">Taille du buffer</param>
		/// <param name="offset">Index (qui ne doit pas être négatif)</param>
		/// <param name="count">Taille (qui ne doit pas être négative)</param>
		[AssertionMethod]
		public static void DoesNotOverflow(int bufferLength, int offset, int count)
		{
			if (offset < 0 || count < 0) throw FailArgumentNotNonNegative(offset < 0 ? "offset" : "count", null);
			if ((bufferLength - offset) < count) throw FailBufferTooSmall("count", null);
		}

		/// <summary>Vérifie qu'une couple index/count ne débord pas d'un buffer, et qu'il n'est pas null</summary>
		/// <param name="bufferLength">Taille du buffer</param>
		/// <param name="offset">Index (qui ne doit pas être négatif)</param>
		/// <param name="count">Taille (qui ne doit pas être négative)</param>
		[AssertionMethod]
		public static void DoesNotOverflow(long bufferLength, long offset, long count)
		{
			if (offset < 0 || count < 0) throw FailArgumentNotNonNegative(offset < 0 ? "offset" : "count", null);
			if ((bufferLength - offset) < count) throw FailBufferTooSmall("count", null);
		}

		/// <summary>Vérifie qu'une couple index/count ne débord pas d'un buffer, et qu'il n'est pas null</summary>
		/// <param name="buffer">Buffer (qui ne doit pas être null)</param>
		/// <param name="offset">Index (qui ne doit pas être négatif)</param>
		/// <param name="count">Taille (qui ne doit pas être négative)</param>
		/// <param name="message"></param>
		[AssertionMethod]
		public static void DoesNotOverflow<TElement>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)]
			TElement[] buffer,
			int offset,
			int count,
			string? message = null)
		{
			if (buffer == null) throw FailArgumentNull("buffer", message);
			if (offset < 0 || count < 0) throw FailArgumentNotNonNegative(offset < 0 ? "offset" : "count", message);
			if ((buffer.Length - offset) < count) throw FailBufferTooSmall("count", message);
		}

		/// <summary>Vérifie qu'une couple index/count ne débord pas d'un buffer, et qu'il n'est pas null</summary>
		/// <param name="buffer">Buffer (qui ne doit pas être null)</param>
		/// <param name="message"></param>
		public static void DoesNotOverflow<TElement>(ArraySegment<TElement> buffer, string? message = null)
		{
			if (buffer.Offset < 0 || buffer.Count < 0) throw FailArgumentNotNonNegative(buffer.Offset < 0 ? "offset" : "count", message);
			if (buffer.Count > 0)
			{
				if (buffer.Array == null) throw FailBufferNull("buffer", message);
				if ((buffer.Array.Length - buffer.Offset) < buffer.Count) throw FailBufferTooSmall("count", message);
			}
			else
			{
				if (buffer.Array != null && buffer.Array.Length < buffer.Offset) throw FailBufferTooSmall("count", message);
			}
		}

		/// <summary>Vérifie qu'une couple index/count ne débord pas d'un buffer, et qu'il n'est pas null</summary>
		/// <param name="buffer">Buffer (qui ne doit pas être null)</param>
		/// <param name="offset">Index (qui ne doit pas être négatif)</param>
		/// <param name="count">Taille (qui ne doit pas être négative)</param>
		/// <param name="message"></param>
		[AssertionMethod]
		public static void DoesNotOverflow<TElement>(
#if USE_ANNOTATIONS
			[System.Diagnostics.CodeAnalysis.NotNull]
#endif
			[AssertionCondition(AssertionConditionType.IS_NOT_NULL)] ICollection<TElement> buffer,
			int offset,
			int count,
			string? message = null)
		{
			if (buffer == null) throw FailArgumentNull("buffer", message);
			if (offset < 0 || count < 0) throw FailArgumentNotNonNegative(offset < 0 ? "offset" : "count", message);
			if ((buffer.Count - offset) < count) throw FailBufferTooSmall("count", message);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailBufferTooSmall(string paramName, string? message = null)
		{
			return ReportFailure(typeof(ArgumentException), ContractMessages.OffsetMustBeWithinBuffer, message, paramName, ContractMessages.ConditionArgBufferOverflow);
		}

		#endregion

		#endregion

		#region Internal Helpers...

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNull(string paramName)
		{
			return ReportFailure(typeof(ArgumentNullException), ContractMessages.ValueCannotBeNull, null, paramName, ContractMessages.ConditionNotNull);
		}

		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		public static Exception FailArgumentNull(string paramName, string? message)
		{
			return ReportFailure(typeof(ArgumentNullException), ContractMessages.ValueCannotBeNull, message, paramName, ContractMessages.ConditionNotNull);
		}

		/// <summary>Déclenche une exception suite à l'échec d'une condition</summary>
		[Pure, MethodImpl(MethodImplOptions.NoInlining)]
		internal static Exception ReportFailure(Type exceptionType, string msg, string? userMessage, string? paramName, string? conditionTxt)
		{
			if (conditionTxt != null && conditionTxt.IndexOf('{') >= 0)
			{ // il y a peut être un "{0}" dans la condition qu'il faut remplacer le nom du paramètre
				conditionTxt = string.Format(conditionTxt, paramName ?? "value");
			}

			string? str = SRC.ContractHelper.RaiseContractFailedEvent(SDC.ContractFailureKind.Precondition, userMessage ?? msg, conditionTxt, null);
			// si l'appelant retourne null, c'est qu'il a lui même traité l'incident ...
			// mais ca n'empêche pas qu'on doit quand même stopper l'exécution !
#if DEBUG
			if (str != null)
			{
				// note: on ne spam les logs si on est en train de unit tester ! (vu qu'on va provoquer intentionnellement plein d'erreurs!)
				if (!IsUnitTesting)
				{
					System.Diagnostics.Debug.Fail(str);
				}
			}
#endif
			string description = userMessage ?? str ?? msg;

			var exception = ThrowHelper.TryMapToKnownException(exceptionType, description, paramName);

			if (exception == null)
			{ // c'est un type compliqué ??
				exception = ThrowHelper.TryMapToComplexException(exceptionType, description, paramName);
			}

			if (exception == null)
			{ // uh? on va quand même envoyer une exception proxy !
				exception = FallbackForUnknownException(description, paramName);
			}

			return exception;
		}

		private static Exception FallbackForUnknownException(string description, string? paramName)
		{
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break(); // README: Si vous tombez ici, c'est que l'appelant a spécifié un type d'Exception qu'on n'arrive pas a construire! il faudrait peut être changer le type...
#endif
			if (paramName != null)
				return new ArgumentException(description, paramName);
			else
				return new InvalidOperationException(description);
		}

		/// <summary>Signale l'échec d'une condition en déclenchant une ContractException</summary>
		/// <remarks>Si un debugger est attaché, un breakpoint est déclenché. Sinon, une ContractException est générée</remarks>
		[Pure]
		[MethodImpl(MethodImplOptions.NoInlining)]
		[DebuggerNonUserCode]
		internal static Exception RaiseContractFailure(SDC.ContractFailureKind kind, string? msg, string? conditionText = null)
		{
			//note: actuellement dans .NET Core 3.x, si conditionText == null, le message formaté ne contient pas la partie "kind" !
			// => le contournement est de passer le message a la place de la condition, ce qui change légèrement la string générée, mais reste lisible!
			string? str = conditionText == null
				? SRC.ContractHelper.RaiseContractFailedEvent(kind, null, msg, null)
				: SRC.ContractHelper.RaiseContractFailedEvent(kind, msg, conditionText, null);
			if (str != null)
			{
				if (IsUnitTesting)
				{
					// throws une AssertionException si on a réussi a se connecter avec NUnit
					var ex = MapToNUnitAssertion(str);
#if DEBUG
					// README: Si vous break-pointez ici, il faut remonter plus haut dans la callstack, et trouver la fonction invoque Contract.xxx(...)
					if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
					// note: à partir de VS 2015 Up2, [DebuggerNonUserCode] n'est plus respecté si la regkey AlwaysEnableExceptionCallbacksOutsideMyCode n'est pas égale à 1, pour améliorer les perfs.
					// cf "How to Suppress Ignorable Exceptions with DebuggerNonUserCode" dans https://blogs.msdn.microsoft.com/visualstudioalm/2016/02/12/using-the-debuggernonusercode-attribute-in-visual-studio-2015/
#endif
					if (ex != null) return ex;
					// sinon, on continue
				}
#if DEBUG
				else if (kind == SDC.ContractFailureKind.Assert && Debugger.IsAttached)
				{
					// uniquement si on F5 depuis VS, car sinon cela cause problèmes avec le TestRunner de R# (qui popup les assertion fail!)
					System.Diagnostics.Debug.Fail(str);
				}
#endif

				return new ContractException(kind, str, msg, conditionText, null);
			}
			//note: on doit quand même retourner quelque chose!
			return new ContractException(kind, "Contract Failed", msg, conditionText, null);
		}

		#endregion

	}

}

#endif
