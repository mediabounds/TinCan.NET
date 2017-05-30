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
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class Verb : JsonModel
    {
        public Uri id { get; set; }
        public LanguageMap display { get; set; }

        public Verb() {}

        public Verb(StringOfJSON json): this(json.toJObject()) {}

        public Verb(JObject jobj)
        {
            if (jobj["id"] != null)
            {
                id = new Uri(jobj.Value<string>("id"));
            }
            if (jobj["display"] != null)
            {
                display = (LanguageMap)jobj.Value<JObject>("display");
            }
        }

        public Verb(Uri uri)
        {
            id = uri;
        }

        public Verb(Uri uri, string defaultLanguage, string defaultTerm)
        {
            id = uri;
            display = new LanguageMap();
            display.Add(defaultLanguage, defaultTerm);
        }

        public Verb(string str)
        {
            id = new Uri (str);
        }

        public override JObject ToJObject(TCAPIVersion version) {
            var result = new JObject();
            if (id != null)
            {
                result.Add("id", id.ToString());
            }

            if (display != null && ! display.isEmpty())
            {
                result.Add("display", display.ToJObject(version));
            }

            return result;
        }

        public static explicit operator Verb(JObject jobj)
        {
            return new Verb(jobj);
        }

        public override string ToString()
        {
            return string.Format("[Verb: id={0}, display={1}]", id, display);
        }

        public static readonly Verb Completed = new Verb(new Uri("http://adlnet.gov/expapi/verbs/completed"), "en-US", "completed");
        public static readonly Verb Terminated = new Verb(new Uri("http://adlnet.gov/expapi/verbs/terminated"), "en-US", "terminated");
        public static readonly Verb Launched = new Verb(new Uri("http://adlnet.gov/expapi/verbs/launched"), "en-US", "launched");
        public static readonly Verb Suspended = new Verb(new Uri("http://adlnet.gov/expapi/verbs/suspended"), "en-US", "suspended");
        public static readonly Verb Favorited = new Verb(new Uri("http://activitystrea.ms/schema/1.0/favorite"), "en-US", "favorited");
        public static readonly Verb Unfavorited = new Verb(new Uri("http://activitystrea.ms/schema/1.0/unfavorite"), "en-US", "unfavorited");
    }
}