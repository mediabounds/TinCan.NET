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
namespace TinCan.Tests
{
    using Xunit;
    using TinCan;

    public class ActivityTest
    {
        [Fact]
        public void TestActivityIdTrailingSlash()
        {
            var activity = new Activity();
            string noTrailingSlash = "http://foo";
            activity.id = noTrailingSlash;
            Assert.Equal(noTrailingSlash, activity.id);
        }

        [Fact]
        public void TestActivityIdCase()
        {
            var activity = new Activity();
            string mixedCase = "http://fOO";
            activity.id = mixedCase;
            Assert.Equal(mixedCase, activity.id);
        }

        [Fact]
        public void TestActivityIdInvalidUri()
        {
            Assert.Throws<System.UriFormatException>(
                () =>
                {
                    var activity = new Activity();
                    string invalid = "foo";
                    activity.id = invalid;
                }
            );
        }
    }
}
