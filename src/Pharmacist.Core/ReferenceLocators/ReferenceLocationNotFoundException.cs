// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Pharmacist.Core.ReferenceLocators
{
    /// <summary>
    /// Exception that happens when a reference cannot be located.
    /// </summary>
    [Serializable]
    public class ReferenceLocationNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceLocationNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ReferenceLocationNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceLocationNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">A inner exception with more error details.</param>
        public ReferenceLocationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceLocationNotFoundException"/> class.
        /// </summary>
        public ReferenceLocationNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceLocationNotFoundException"/> class.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The serialization context.</param>
        protected ReferenceLocationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
