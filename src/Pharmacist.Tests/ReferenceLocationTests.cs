// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Pharmacist.Core.ReferenceLocators;

using Xunit;

namespace Pharmacist.Tests
{
    public class ReferenceLocationTests
    {
        [Fact]
        public void GetsValidLocation()
        {
            var location = ReferenceLocator.GetReferenceLocation();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Contains("Visual Studio", location);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.Contains("/Library⁩/Frameworks⁩/Libraries/⁨mono", location);
            }
        }
    }
}
