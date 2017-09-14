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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TinCan.Documents;
using TinCan.LRSResponses;

namespace TinCan
{
    public class RemoteLRS : ILRS
    {
        SemaphoreSlim makeRequestSemaphore = new SemaphoreSlim(1, 1);
        public Uri endpoint { get; set; }
        public TCAPIVersion version { get; set; }
        public string auth { get; set; }
        public Dictionary<string, string> extended { get; set; }
        readonly HttpClient client = new HttpClient();

        public void SetAuth(string username, string password)
        {
            auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
        }

        public RemoteLRS() { }

        public RemoteLRS(Uri endpoint, TCAPIVersion version, string username, string password)
        {
            this.endpoint = endpoint;
            this.version = version;
            SetAuth(username, password);
        }

        public RemoteLRS(string endpoint, TCAPIVersion version, string username, string password) : this(new Uri(endpoint), version, username, password) { }
        public RemoteLRS(string endpoint, string username, string password) : this(endpoint, TCAPIVersion.latest(), username, password) { }

        class MyHTTPRequest
        {
            public HttpMethod method { get; set; }
            public string resource { get; set; }
            public Dictionary<string, string> queryParams { get; set; }
            public Dictionary<string, string> headers { get; set; }
            public string contentType { get; set; }
            public byte[] content { get; set; }

            public override string ToString()
            {
                return string.Format(
                    "[MyHTTPRequest: method={0}, resource={1}, queryParams={2}, headers={3}, contentType={4}, content={5}]",
                    method, resource, string.Join(";", queryParams), headers, contentType, Encoding.UTF8.GetString(content, 0, content.Length));
            }
        }

        public enum RequestType { post, put };

        class MyHTTPResponse
        {
            public HttpStatusCode status { get; set; }
            public string contentType { get; set; }
            public byte[] content { get; set; }
            public DateTime lastModified { get; set; }
            public string etag { get; set; }
            public Exception ex { get; set; }

            public MyHTTPResponse() { }
            public MyHTTPResponse(HttpResponseMessage webResp)
            {
                status = webResp.StatusCode;

                if (webResp?.Content?.Headers?.ContentType != null)
                {
                    contentType = webResp.Content.Headers.ContentType.ToString();
                }

                if (webResp.Headers.ETag != null)
                {
                    etag = webResp.Headers.ETag.ToString();
                }

                if (webResp?.Content?.Headers?.LastModified != null)
                {
                    lastModified = webResp.Content.Headers.LastModified.Value.LocalDateTime;
                }

                content = webResp.Content.ReadAsByteArrayAsync().Result;
            }

            public override string ToString()
            {
                return string.Format("[MyHTTPResponse: status={0}, contentType={1}, content={2}, lastModified={3}, etag={4}, ex={5}]",
                                     status, contentType, Encoding.UTF8.GetString(content, 0, content.Length), lastModified, etag, ex);
            }
        }

        async Task<MyHTTPResponse> MakeRequest(MyHTTPRequest req)
        {
            string url;
            if (req.resource.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = req.resource;
            }
            else
            {
                url = endpoint.ToString();
                if (!url.EndsWith("/", StringComparison.Ordinal) && !req.resource.StartsWith("/", StringComparison.Ordinal)) {
                    url += "/";
                }
                url += req.resource;
            }

            if (req.queryParams != null)
            {
                string qs = "";
                foreach (KeyValuePair<string, string> entry in req.queryParams)
                {
                    if (qs != "")
                    {
                        qs += "&";
                    }
                    qs += WebUtility.UrlEncode(entry.Key) + "=" + WebUtility.UrlEncode(entry.Value);
                }
                if (qs != "")
                {
                    url += "?" + qs;
                }
            }

            var webReq = new HttpRequestMessage(req.method, new Uri(url));

            // We only have one client. We cannot modify it while its in use.
            await makeRequestSemaphore.WaitAsync();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-Experience-API-Version", version.ToString());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(req.contentType ?? "application/content-stream"));

            if (auth != null)
            {
                client.DefaultRequestHeaders.Add("Authorization", auth);
            }
            if (req.headers != null)
            {
                foreach (var entry in req.headers)
                {
                    if (client.DefaultRequestHeaders.Contains(entry.Key))
                    {
                        makeRequestSemaphore.Release();
                        throw new InvalidOperationException($"Tried to add duplicate entry {entry.Key} to request headers with value {entry.Value}; previous value {client.DefaultRequestHeaders.GetValues(entry.Key)}");
                    }

                    client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                }
            }

            if (req.content != null)
            {
                webReq.Content = new ByteArrayContent(req.content);
                webReq.Content.Headers.Add("Content-Length", req.content.Length.ToString());
                webReq.Content.Headers.Add("Content-Type", req.contentType ?? "text/plain");
            }

            MyHTTPResponse resp;

            try
            {
                var response = await client.SendAsync(webReq).ConfigureAwait(false);
                resp = new MyHTTPResponse(response);
            }
            catch (HttpRequestException ex)
            {
                resp = new MyHTTPResponse();

                if (ex.Message != null)
                {
                    resp.content = Encoding.UTF8.GetBytes(ex.Message);
                    resp.contentType = "text/plain";
                }
                else
                {
                    resp.content = Encoding.UTF8.GetBytes("HttpRequestException without message");
                }
                resp.ex = ex;
            }
            finally
            {
                makeRequestSemaphore.Release();
            }

            return resp;
        }

        /// <summary>
        /// See http://www.yoda.arachsys.com/csharp/readbinary.html no license found
        ///
        /// Reads data from a stream until the end is reached. The
        /// data is returned as a byte array. An IOException is
        /// thrown if any of the underlying IO calls fail.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        /// <param name="initialLength">The initial buffer length</param>
        static byte[] ReadFully(Stream stream, int initialLength)
        {
            // If we've been passed an unhelpful initial length, just
            // use 32K.
            if (initialLength < 1)
            {
                initialLength = 32768;
            }

            var buffer = new byte[initialLength];
            int read = 0;

            int chunk;
            while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;

                // If we've reached the end of our buffer, check to see if there's
                // any more information
                if (read == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    // End of stream? If so, we're done
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    // Nope. Resize the buffer, put in the byte we've just
                    // read, and continue
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[read] = (byte)nextByte;
                    buffer = newBuffer;
                    read++;
                }
            }
            // Buffer is now too big. Shrink it.
            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);
            return ret;
        }

        async Task<MyHTTPResponse> GetDocument(string resource, Dictionary<string, string> queryParams, Document document)
        {
            var req = new MyHTTPRequest();
            req.method = HttpMethod.Get;
            req.resource = resource;
            req.queryParams = queryParams;
            var res = await MakeRequest(req);
            if (res.status == HttpStatusCode.OK)
            {
                document.content = res.content;
                document.contentType = res.contentType;
                document.timestamp = res.lastModified;
                document.etag = res.etag;
            }

            return res;
        }

        async Task<ProfileKeysLRSResponse> GetProfileKeys(string resource, Dictionary<string, string> queryParams)
        {
            var r = new ProfileKeysLRSResponse();

            var req = new MyHTTPRequest();
            req.method = HttpMethod.Get;
            req.resource = resource;
            req.queryParams = queryParams;

            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            r.success = true;

            var keys = JArray.Parse(Encoding.UTF8.GetString(res.content, 0, res.content.Length));
            if (keys.Count > 0) {
                r.content = new List<string>();
                foreach (JToken key in keys) {
                    r.content.Add((string)key);
                }
            }

            return r;
        }

        async Task<LRSResponse> SaveDocument(string resource, Dictionary<string, string> queryParams, Document document, RequestType requestType = RequestType.put )
        {
            var r = new LRSResponse();

            var req = new MyHTTPRequest();
            if (requestType == RequestType.post)
            {
                req.method = HttpMethod.Post;
            }
            else
            {
                req.method = HttpMethod.Put;
            }
            req.resource = resource;
            req.queryParams = queryParams;
            req.contentType = document.contentType;
            req.content = document.content;
            if (document.etag != null)
            {
                req.headers = new Dictionary<string, string>();
                req.headers.Add("If-Match", document.etag);
            }
            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.NoContent)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            r.success = true;

            return r;
        }

        async Task<LRSResponse> DeleteDocument(string resource, Dictionary<string, string> queryParams)
        {
            var r = new LRSResponse();

            var req = new MyHTTPRequest();
            req.method = HttpMethod.Delete;
            req.resource = resource;
            req.queryParams = queryParams;

            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.NoContent)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            r.success = true;

            return r;
        }

        async Task<StatementLRSResponse> GetStatement(Dictionary<string, string> queryParams)
        {
            var r = new StatementLRSResponse();

            var req = new MyHTTPRequest();
            req.method = HttpMethod.Get;
            req.resource = "statements";
            req.queryParams = queryParams;

            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            r.success = true;
            r.content = new Statement(new Json.StringOfJSON(Encoding.UTF8.GetString(res.content, 0, res.content.Length)));

            return r;
        }

        public async Task<AboutLRSResponse> About()
        {
            var r = new AboutLRSResponse();

            var req = new MyHTTPRequest();
            req.method = HttpMethod.Get;
            req.resource = "about";

            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            r.success = true;
            r.content = new About(Encoding.UTF8.GetString(res.content, 0, res.content.Length));

            return r;
        }

        public async Task<StatementLRSResponse> SaveStatement(Statement statement)
        {
            var r = new StatementLRSResponse();
            var req = new MyHTTPRequest();
            req.queryParams = new Dictionary<string, string>();
            req.resource = "statements";

            if (statement.id == null)
            {
                req.method = HttpMethod.Post;
            }
            else
            {
                req.method = HttpMethod.Put;
                req.queryParams.Add("statementId", statement.id.ToString());
            }

            req.contentType = "application/json";
            req.content = Encoding.UTF8.GetBytes(statement.ToJSON(version));

            var res = await MakeRequest(req);
            if (statement.id == null)
            {
                if (res.status != HttpStatusCode.OK)
                {
                    r.success = false;
                    r.httpException = res.ex;
                    r.SetErrMsgFromBytes(res.content, (int)res.status);
                    return r;
                }

                var ids = JArray.Parse(Encoding.UTF8.GetString(res.content, 0, res.content.Length));
                statement.id = new Guid((string)ids[0]);
            }
            else {
                if (res.status != HttpStatusCode.NoContent)
                {
                    r.success = false;
                    r.httpException = res.ex;
                    r.SetErrMsgFromBytes(res.content);
                    return r;
                }
            }

            r.success = true;
            r.content = statement;

            return r;
        }

        public async Task<StatementLRSResponse> VoidStatement(Guid id, Agent agent)
        {
            var voidStatement = new Statement
            {
                actor = agent,
                verb = new Verb
                {
                    id = new Uri("http://adlnet.gov/expapi/verbs/voided"),
                    display = new LanguageMap()
                },
                target = new StatementRef { id = id }
            };
            voidStatement.verb.display.Add("en-US", "voided");

            return await SaveStatement(voidStatement);
        }

        public async Task<StatementsResultLRSResponse> SaveStatements(List<Statement> statements)
        {
            var r = new StatementsResultLRSResponse();

            var req = new MyHTTPRequest();
            req.resource = "statements";
            req.method = HttpMethod.Post;
            req.contentType = "application/json";

            var jarray = new JArray();
            foreach (Statement st in statements)
            {
                jarray.Add(st.ToJObject(version));
            }
            req.content = Encoding.UTF8.GetBytes(jarray.ToString());

            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            var ids = JArray.Parse(Encoding.UTF8.GetString(res.content, 0, res.content.Length));
            for (int i = 0; i < ids.Count; i++)
            {
                statements[i].id = new Guid((string)ids[i]);
            }

            r.success = true;
            r.content = new StatementsResult(statements);

            return r;
        }

        public async Task<StatementLRSResponse> RetrieveStatement(Guid id)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("statementId", id.ToString());

            return await GetStatement(queryParams);
        }

        public async Task<StatementLRSResponse> RetrieveVoidedStatement(Guid id)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("voidedStatementId", id.ToString());

            return await GetStatement(queryParams);
        }

        public async Task<StatementsResultLRSResponse> QueryStatements(StatementsQuery query)
        {
            var r = new StatementsResultLRSResponse();

            var req = new MyHTTPRequest();
            req.method = HttpMethod.Get;
            req.resource = "statements";
            req.queryParams = query.ToParameterMap(version);

            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            r.success = true;
            r.content = new StatementsResult(new Json.StringOfJSON(Encoding.UTF8.GetString(res.content, 0, res.content.Length)));

            return r;
        }

        public async Task<StatementsResultLRSResponse> MoreStatements(StatementsResult result)
        {
            var r = new StatementsResultLRSResponse();

            var req = new MyHTTPRequest();
            req.method = HttpMethod.Get;
            req.resource = endpoint.Authority;
            if (!req.resource.EndsWith("/", StringComparison.Ordinal)) {
                req.resource += "/";
            }
            req.resource += result.more;

            var res = await MakeRequest(req);
            if (res.status != HttpStatusCode.OK)
            {
                r.success = false;
                r.httpException = res.ex;
                r.SetErrMsgFromBytes(res.content, (int)res.status);
                return r;
            }

            r.success = true;
            r.content = new StatementsResult(new Json.StringOfJSON(Encoding.UTF8.GetString(res.content, 0, res.content.Length)));

            return r;
        }

        public async Task<ProfileKeysLRSResponse> RetrieveStateIds(Activity activity, Agent agent, Guid? registration = null)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("activityId", activity.id);
            queryParams.Add("agent", agent.ToJSON(version));
            if (registration != null)
            {
                queryParams.Add("registration", registration.ToString());
            }

            return await GetProfileKeys("activities/state", queryParams);
        }

        public async Task<StateLRSResponse> RetrieveState(string id, Activity activity, Agent agent, Guid? registration = null)
        {
            var r = new StateLRSResponse();

            var queryParams = new Dictionary<string, string>();
            queryParams.Add("stateId", id);
            queryParams.Add("activityId", activity.id);
            queryParams.Add("agent", agent.ToJSON(version));

            var state = new StateDocument();
            state.id = id;
            state.activity = activity;
            state.agent = agent;

            if (registration != null)
            {
                queryParams.Add("registration", registration.ToString());
                state.registration = registration;
            }

            var resp = await GetDocument("activities/state", queryParams, state);
            if (resp.status != HttpStatusCode.OK && resp.status != HttpStatusCode.NotFound)
            {
                r.success = false;
                r.httpException = resp.ex;
                r.SetErrMsgFromBytes(resp.content);
                return r;
            }
            r.success = true;
            r.content = state;

            return r;
        }

        public async Task<LRSResponse> SaveState(StateDocument state)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("stateId", state.id);
            queryParams.Add("activityId", state.activity.id);
            queryParams.Add("agent", state.agent.ToJSON(version));

            if (state.registration != null)
            {
                queryParams.Add("registration", state.registration.ToString());
            }

            return await SaveDocument("activities/state", queryParams, state);
        }

        public async Task<LRSResponse> DeleteState(StateDocument state)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("stateId", state.id);
            queryParams.Add("activityId", state.activity.id);
            queryParams.Add("agent", state.agent.ToJSON(version));

            if (state.registration != null)
            {
                queryParams.Add("registration", state.registration.ToString());
            }

            return await DeleteDocument("activities/state", queryParams);
        }

        public async Task<LRSResponse> ClearState(Activity activity, Agent agent, Guid? registration = null)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("activityId", activity.id);
            queryParams.Add("agent", agent.ToJSON(version));

            if (registration != null)
            {
                queryParams.Add("registration", registration.ToString());
            }

            return await DeleteDocument("activities/state", queryParams);
        }

        public async Task<ProfileKeysLRSResponse> RetrieveActivityProfileIds(Activity activity)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("activityId", activity.id);

            return await GetProfileKeys("activities/profile", queryParams);
        }

        public async Task<ActivityProfileLRSResponse> RetrieveActivityProfile(string id, Activity activity)
        {
            var r = new ActivityProfileLRSResponse();

            var queryParams = new Dictionary<string, string>();
            queryParams.Add("profileId", id);
            queryParams.Add("activityId", activity.id);

            var profile = new ActivityProfileDocument();
            profile.id = id;
            profile.activity = activity;

            var resp = await GetDocument("activities/profile", queryParams, profile);
            if (resp.status != HttpStatusCode.OK && resp.status != HttpStatusCode.NotFound)
            {
                r.success = false;
                r.httpException = resp.ex;
                r.SetErrMsgFromBytes(resp.content);
                return r;
            }
            r.success = true;
            r.content = profile;

            return r;
        }

        public async Task<LRSResponse> SaveActivityProfile(ActivityProfileDocument profile)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("activityId", profile.activity.id);

            return await SaveDocument("activities/profile", queryParams, profile);
        }

        public async Task<LRSResponse> DeleteActivityProfile(ActivityProfileDocument profile)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("activityId", profile.activity.id);

            return await DeleteDocument("activities/profile", queryParams);
        }

        public async Task<ProfileKeysLRSResponse> RetrieveAgentProfileIds(Agent agent)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("agent", agent.ToJSON(version));

            return await GetProfileKeys("agents/profile", queryParams);
        }

        public async Task<AgentProfileLRSResponse> RetrieveAgentProfile(string id, Agent agent)
        {
            var r = new AgentProfileLRSResponse();

            var queryParams = new Dictionary<string, string>();
            queryParams.Add("profileId", id);
            queryParams.Add("agent", agent.ToJSON(version));

            var profile = new AgentProfileDocument();
            profile.id = id;
            profile.agent = agent;

            var resp = await GetDocument("agents/profile", queryParams, profile);

            if (resp.status != HttpStatusCode.OK && resp.status != HttpStatusCode.NotFound)
            {
                r.success = false;
                r.httpException = resp.ex;
                r.SetErrMsgFromBytes(resp.content);
                return r;
            }

            profile.content = resp?.content;
            profile.contentType = resp?.contentType;
            profile.etag = resp?.etag;

            r.success = true;
            r.content = profile;

            return r;
        }

        public async Task<LRSResponse> SaveAgentProfile(AgentProfileDocument profile)
        {
            return await SaveAgentProfile(profile, RequestType.put);
        }

        public async Task<LRSResponse> ForceSaveAgentProfile(AgentProfileDocument profile)
        {
            return await SaveAgentProfile(profile, RequestType.post);
        }

        async Task<LRSResponse> SaveAgentProfile(AgentProfileDocument profile, RequestType requestType)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("agent", profile.agent.ToJSON(version));
            return await SaveDocument("agents/profile", queryParams, profile, requestType);
        }

        public async Task<LRSResponse> DeleteAgentProfile(AgentProfileDocument profile)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("profileId", profile.id);
            queryParams.Add("agent", profile.agent.ToJSON(version));

            return await DeleteDocument("agents/profile", queryParams);
        }
    }
}
