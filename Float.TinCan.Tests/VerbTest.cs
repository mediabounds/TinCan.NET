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
    using Xunit;
    using Newtonsoft.Json.Linq;
    using TinCan;
    using TinCan.Json;

    public class VerbTest
    {
        [Fact]
        public void TestEmptyCtr()
        {
            Verb obj = new Verb();
            Assert.IsType<Verb>(obj);
            Assert.Null(obj.id);
            Assert.Null(obj.display);

            //StringAssert.AreEqualIgnoringCase("{}", obj.ToJSON());
        }

        [Fact]
        public void TestJObjectCtr()
        {
            String id = "http://adlnet.gov/expapi/verbs/experienced";

            JObject cfg = new JObject();
            cfg.Add("id", id);

            Verb obj = new Verb(cfg);
            Assert.IsType<Verb>(obj);
            //Assert.Equal(obj.ToJSON(), Is.EqualTo("{\"id\":\"" + id + "\"}"));
        }

        [Fact]
        public void TestStringOfJSONCtr()
        {
            String id = "http://adlnet.gov/expapi/verbs/experienced";
            String json = "{\"id\":\"" + id + "\"}";
            StringOfJSON strOfJson = new StringOfJSON(json);

            Verb obj = new Verb(strOfJson);
            Assert.IsType<Verb>(obj);
            //Assert.That(obj.ToJSON(), Is.EqualTo(json));
        }
    }
}
