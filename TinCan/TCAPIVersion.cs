﻿/*
    Copyright 2014 Rustici Software

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
using System;
using System.Collections.Generic;

namespace TinCan
{
    public sealed class TCAPIVersion
    {
        public static readonly TCAPIVersion V103 = new TCAPIVersion("1.0.3");
        public static readonly TCAPIVersion V102 = new TCAPIVersion("1.0.2");
        public static readonly TCAPIVersion V101 = new TCAPIVersion("1.0.1");
        public static readonly TCAPIVersion V100 = new TCAPIVersion("1.0.0");
        public static readonly TCAPIVersion V095 = new TCAPIVersion("0.95");
        public static readonly TCAPIVersion V090 = new TCAPIVersion("0.9");

        public static TCAPIVersion latest()
        {
            return V101;
        }

        static Dictionary<string, TCAPIVersion> known;
        static Dictionary<string, TCAPIVersion> supported;

        public static Dictionary<string, TCAPIVersion> GetKnown()
        {
            if (known != null) {
                return known;
            }

            known = new Dictionary<string, TCAPIVersion>();
            known.Add("1.0.3", V103);
            known.Add("1.0.2", V102);
            known.Add("1.0.1", V101);
            known.Add("1.0.0", V100);
            known.Add("0.95", V095);
            known.Add("0.9", V090);

            return known;
        }

        public static Dictionary<string, TCAPIVersion> GetSupported()
        {
            if (supported != null) {
                return supported;
            }

            supported = new Dictionary<string, TCAPIVersion>();
            supported.Add("1.0.3", V103);
            supported.Add("1.0.2", V102);
            supported.Add("1.0.1", V101);
            supported.Add("1.0.0", V100);

            return supported;
        }

        public static explicit operator TCAPIVersion(string vStr)
        {
            var s = GetKnown();
            if (!s.ContainsKey(vStr))
            {
                throw new ArgumentException("Unrecognized version: " + vStr);
            }

            return s[vStr];
        }

        readonly string text;

        TCAPIVersion(string value)
        {
            text = value;
        }

        public override string ToString()
        {
            return text;
        }
    }
}
