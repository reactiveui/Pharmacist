// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Pharmacist.Common
{
    /// <summary>
    /// A subject which will have a single awaiter.
    /// </summary>
    /// <typeparam name="T">The type of signals given by the subject.</typeparam>
    public sealed class SingleAwaitSubject<T> : ISubject<T>, IDisposable
    {
        private readonly Subject<T> _inner = new Subject<T>();

        /// <summary>
        /// Gets the awaiter based on the first item in the collection.
        /// </summary>
        /// <returns>The async subject awaiter.</returns>
        public AsyncSubject<T> GetAwaiter()
        {
            return _inner.Take(1).GetAwaiter();
        }

        /// <inheritdoc/>
        public void OnNext(T value)
        {
            _inner.OnNext(value);
        }

        /// <inheritdoc/>
        public void OnError(Exception error)
        {
            _inner.OnError(error);
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
            _inner.OnCompleted();
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _inner.Subscribe(observer);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _inner?.Dispose();
        }
    }
}
