/*
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
    using System;
    using System.Collections.Generic;
    using Xunit;
    using Newtonsoft.Json.Linq;
    using TinCan;
    using TinCan.Json;

    public class SubStatementTest
    {
        [Fact]
        public void TestEmptyCtr()
        {
            var obj = new SubStatement();
            Assert.IsType<SubStatement>(obj);
            Assert.Null(obj.actor);
            Assert.Null(obj.verb);
            Assert.Null(obj.target);
            Assert.Null(obj.result);
            Assert.Null(obj.context);

            //StringAssert.AreEqualIgnoringCase("{\"objectType\":\"SubStatement\"}", obj.ToJSON());
        }

        [Fact]
        public void TestJObjectCtrNestedSubStatement()
        {
            JObject cfg = new JObject();
            cfg.Add("actor", Support.agent.ToJObject());
            cfg.Add("verb", Support.verb.ToJObject());
            cfg.Add("object", Support.subStatement.ToJObject());

            var obj = new SubStatement(cfg);
            Assert.IsType<SubStatement>(obj);
            Assert.Null(obj.target);
        }
    }
}
