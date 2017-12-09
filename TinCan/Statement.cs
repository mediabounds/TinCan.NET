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
using Newtonsoft.Json.Linq;
using TinCan.Json;

namespace TinCan
{
    public class Statement : StatementBase
    {
        const string ISODateTimeFormat = "o";

        public Guid? id { get; set; }
        public DateTime? stored { get; set; }
        public Agent authority { get; set; }
        public TCAPIVersion version { get; set; }
        //public List<Attachment> attachments { get; set; }

        public Statement() { }
        public Statement(StringOfJSON json) : this(json.toJObject()) { }

        public Statement(JObject jobj) : base(jobj) {
            if (jobj["id"] != null)
            {
                id = new Guid(jobj.Value<string>("id"));
            }
            if (jobj["stored"] != null)
            {
                stored = jobj.Value<DateTime>("stored");
            }
            if (jobj["authority"] != null)
            {
                authority = (Agent)jobj.Value<JObject>("authority");
            }
            if (jobj["version"] != null)
            {
                version = (TCAPIVersion)jobj.Value<string>("version");
            }

            //
            // handle SubStatement as target which isn't provided by StatementBase
            // because SubStatements are not allowed to nest
            //
            if (jobj["object"] != null && (string)jobj["object"]["objectType"] == SubStatement.OBJECT_TYPE)
            {
                target = (SubStatement)jobj.Value<JObject>("object");
            }
        }

        public override JObject ToJObject(TCAPIVersion version)
        {
            var resultObject = base.ToJObject(version);

            if (id != null)
            {
                resultObject.Add("id", id.ToString());
            }
            if (stored != null)
            {
                resultObject.Add("stored", stored.Value.ToString(ISODateTimeFormat));
            }
            if (authority != null)
            {
                resultObject.Add("authority", authority.ToJObject(version));
            }
            if (version != null)
            {
                resultObject.Add("version", version.ToString());
            }

            return resultObject;
        }

        public void Stamp()
        {
            if (id == null)
            {
                id = Guid.NewGuid();
            }
            if (timestamp == null)
            {
                timestamp = DateTime.UtcNow;
            }
        }

        public override string ToString()
        {
            return string.Format("[Statement: id={0}, stored={1}, authority={2}, version={3}, base={4}]", id, stored, authority, version, base.ToString());
        }
    }
}
