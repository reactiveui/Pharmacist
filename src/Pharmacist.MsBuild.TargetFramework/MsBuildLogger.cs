// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Microsoft.Build.Utilities;
using Splat;

namespace Pharmacist.MsBuild.TargetFramework
{
    /// <summary>
    /// A logger which talks to the MsBuild logging system.
    /// </summary>
    public class MsBuildLogger : ILogger
    {
        private readonly TaskLoggingHelper _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsBuildLogger"/> class.
        /// </summary>
        /// <param name="log">The task logging helper which contains a bunch of logging stuff for us.</param>
        /// <param name="logLevel">The minimum log level to log at.</param>
        public MsBuildLogger(TaskLoggingHelper log, LogLevel logLevel)
        {
            _log = log;
            Level = logLevel;
        }

        /// <inheritdoc />
        public LogLevel Level { get; }

        /// <inheritdoc />
        public void Write(string message, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    _log.LogMessage(message);
                    break;
                case LogLevel.Warn:
                    _log.LogWarning(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    _log.LogError(message);
                    break;
            }
        }

        /// <inheritdoc />
        public void Write(Exception exception, string message, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            _log.LogMessage(message);

            switch (logLevel)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    _log.LogMessage(exception?.ToString());
                    break;
                case LogLevel.Warn:
                    _log.LogWarningFromException(exception);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    _log.LogErrorFromException(exception);
                    break;
            }
        }

        /// <inheritdoc />
        public void Write(string message, Type type, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    _log.LogMessage(message);
                    break;
                case LogLevel.Warn:
                    _log.LogWarning(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    _log.LogError(message);
                    break;
            }
        }

        /// <inheritdoc />
        public void Write(Exception exception, string message, Type type, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level)
            {
                return;
            }

            _log.LogMessage(message);

            switch (logLevel)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    _log.LogMessage(exception?.ToString());
                    break;
                case LogLevel.Warn:
                    _log.LogWarningFromException(exception);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    _log.LogErrorFromException(exception);
                    break;
            }
        }
    }
}
