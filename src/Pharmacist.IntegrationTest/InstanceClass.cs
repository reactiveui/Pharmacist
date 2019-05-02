// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace Pharmacist.IntegrationTest
{
    /// <summary>
    /// Provides test instances of instance class events.
    /// </summary>
    public class InstanceClass
    {
        /// <summary>
        /// An event for actions.
        /// </summary>
        public event Action ActionEvent;

        /// <summary>
        /// Gets an event that has a int parameter.
        /// </summary>
        public event Action<int> IntActionEvent;

        /// <summary>
        /// Gets an event that has a string parameter.
        /// </summary>
        public event Action<int, string> IntStringActionEvent;

        /// <summary>
        /// Gets an event that uses a event handler.
        /// </summary>
        public event EventHandler EventHandlerEvent;

        /// <summary>
        /// Invokes the event.
        /// </summary>
        protected virtual void OnActionEvent()
        {
            ActionEvent?.Invoke();
        }

        /// <summary>
        /// Invokes the event.
        /// </summary>
        /// <param name="obj">The parameter being passed.</param>
        protected virtual void OnIntActionEvent(int obj)
        {
            IntActionEvent?.Invoke(obj);
        }

        /// <summary>
        /// Invokes the event.
        /// </summary>
        /// <param name="arg1">The first parameter being passed.</param>
        /// <param name="arg2">The second parameter being passed.</param>
        protected virtual void OnIntStringActionEvent(int arg1, string arg2)
        {
            IntStringActionEvent?.Invoke(arg1, arg2);
        }

        /// <summary>
        /// Invokes the event.
        /// </summary>
        protected virtual void OnEventHandlerEvent()
        {
            EventHandlerEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
