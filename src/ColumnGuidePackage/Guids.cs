// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;

namespace Microsoft.ColumnGuidePackage
{
    static class GuidList
    {
        public const string guidColumnGuidePkgString = "a0b80b01-be16-4c42-ab44-7f8d057faa2f";
        public const string guidColumnGuideCmdSetString = "5aa4cf31-6030-4655-99e7-239b331103f3";

        public static readonly Guid guidColumnGuideCmdSet = new Guid(guidColumnGuideCmdSetString);
    };
}