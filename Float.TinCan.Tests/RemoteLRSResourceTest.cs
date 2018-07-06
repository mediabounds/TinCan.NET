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
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;
    using Newtonsoft.Json.Linq;
    using TinCan;
    using TinCan.Documents;
    using TinCan.Json;
    using TinCan.LRSResponses;

    public class RemoteLRSResourceTest
    {
        RemoteLRS lrs;

        public RemoteLRSResourceTest()
        {
            //
            // these are credentials used by the other OSS libs when building via Travis-CI
            // so are okay to include in the repository, if you wish to have access to the
            // results of the test suite then supply your own endpoint, username, and password
            //
            lrs = new RemoteLRS(
                "https://cloud.scorm.com/tc/U2S4SI5FY0/sandbox/",
                "Nja986GYE1_XrWMmFUE",
                "Bd9lDr1kjaWWY6RID_4"
            );
        }

        [Fact]
        public async Task TestAbout()
        {
            AboutLRSResponse lrsRes = await lrs.About();
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestAboutFailure()
        {
            lrs.endpoint = new Uri("http://cloud.scorm.com/tc/3TQLAI9/sandbox/");

            AboutLRSResponse lrsRes = await lrs.About();
            Assert.False(lrsRes.success);
            Console.WriteLine("TestAboutFailure - errMsg: " + lrsRes.errMsg);
        }

        [Fact]
        public async Task TestSaveStatement()
        {
            var statement = new Statement();
            statement.actor = Support.agent;
            statement.verb = Support.verb;
            statement.target = Support.activity;

            StatementLRSResponse lrsRes = await lrs.SaveStatement(statement);
            Assert.True(lrsRes.success);
            Assert.Equal(statement, lrsRes.content);
            Assert.NotNull(lrsRes.content.id);
        }

        [Fact]
        public async Task TestSaveStatementWithID()
        {
            var statement = new Statement();
            statement.Stamp();
            statement.actor = Support.agent;
            statement.verb = Support.verb;
            statement.target = Support.activity;

            StatementLRSResponse lrsRes = await lrs.SaveStatement(statement);
            Assert.True(lrsRes.success);
            Assert.Equal(statement, lrsRes.content);
        }

        [Fact]
        public async Task TestSaveStatementStatementRef()
        {
            var statement = new Statement();
            statement.Stamp();
            statement.actor = Support.agent;
            statement.verb = Support.verb;
            statement.target = Support.statementRef;

            StatementLRSResponse lrsRes = await lrs.SaveStatement(statement);
            Assert.True(lrsRes.success);
            Assert.Equal(statement, lrsRes.content);
        }

        [Fact]
        public async Task TestSaveStatementSubStatement()
        {
            var statement = new Statement();
            statement.Stamp();
            statement.actor = Support.agent;
            statement.verb = Support.verb;
            statement.target = Support.subStatement;

            Console.WriteLine(statement.ToJSON(true));

            StatementLRSResponse lrsRes = await lrs.SaveStatement(statement);
            Assert.True(lrsRes.success);
            Assert.Equal(statement, lrsRes.content);
        }

        [Fact]
        public async Task TestVoidStatement()
        {
            Guid toVoid = Guid.NewGuid();
            StatementLRSResponse lrsRes = await lrs.VoidStatement(toVoid, Support.agent);

            Assert.True(lrsRes.success, "LRS response successful");
            Assert.Equal(new Uri("http://adlnet.gov/expapi/verbs/voided"), lrsRes.content.verb.id);
            Assert.Equal(toVoid, ((StatementRef)lrsRes.content.target).id);
        }

        [Fact]
        public async Task TestSaveStatements()
        {
            var statement1 = new Statement();
            statement1.actor = Support.agent;
            statement1.verb = Support.verb;
            statement1.target = Support.parent;

            var statement2 = new Statement();
            statement2.actor = Support.agent;
            statement2.verb = Support.verb;
            statement2.target = Support.activity;
            statement2.context = Support.context;

            var statements = new List<Statement>();
            statements.Add(statement1);
            statements.Add(statement2);

            StatementsResultLRSResponse lrsRes = await lrs.SaveStatements(statements);
            Assert.True(lrsRes.success);
            // TODO: check statements match and ids not null
        }

        [Fact]
        public async Task TestRetrieveStatement()
        {
            var statement = new TinCan.Statement();
            statement.Stamp();
            statement.actor = Support.agent;
            statement.verb = Support.verb;
            statement.target = Support.activity;
            statement.context = Support.context;
            statement.result = Support.result;

            StatementLRSResponse saveRes = await lrs.SaveStatement(statement);
            if (saveRes.success)
            {
                StatementLRSResponse retRes = await lrs.RetrieveStatement(saveRes.content.id.Value);
                Assert.True(retRes.success);
                Console.WriteLine("TestRetrieveStatement - statement: " + retRes.content.ToJSON(true));
            }
            else
            {
                // TODO: skipped?
            }
        }

        [Fact]
        public async Task TestQueryStatements()
        {
            var query = new TinCan.StatementsQuery();
            query.agent = Support.agent;
            query.verbId = Support.verb.id;
            query.activityId = Support.parent.id;
            query.relatedActivities = true;
            query.relatedAgents = true;
            query.format = StatementsQueryResultFormat.IDS;
            query.limit = 10;

            StatementsResultLRSResponse lrsRes = await lrs.QueryStatements(query);
            Assert.True(lrsRes.success);
            Console.WriteLine("TestQueryStatements - statement count: " + lrsRes.content.statements.Count);
        }

        [Fact]
        public async Task TestMoreStatements()
        {
            var query = new TinCan.StatementsQuery();
            query.format = StatementsQueryResultFormat.IDS;
            query.limit = 2;

            StatementsResultLRSResponse queryRes = await lrs.QueryStatements(query);
            if (queryRes.success && queryRes.content.more != null)
            {
                StatementsResultLRSResponse moreRes = await lrs.MoreStatements(queryRes.content);
                Assert.True(moreRes.success);
                Console.WriteLine("TestMoreStatements - statement count: " + moreRes.content.statements.Count);
            }
            else
            {
                // TODO: skipped?
            }
        }

        [Fact]
        public async Task TestRetrieveStateIds()
        {
            ProfileKeysLRSResponse lrsRes = await lrs.RetrieveStateIds(Support.activity, Support.agent);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestRetrieveState()
        {
            StateLRSResponse lrsRes = await lrs.RetrieveState("test", Support.activity, Support.agent);
            Assert.True(lrsRes.success);
            Assert.IsType<StateDocument>(lrsRes.content);
        }

        [Fact]
        public async Task TestSaveState()
        {
            var doc = new StateDocument();
            doc.activity = Support.activity;
            doc.agent = Support.agent;
            doc.id = "test";
            doc.content = System.Text.Encoding.UTF8.GetBytes("Test value");

            LRSResponse lrsRes = await lrs.SaveState(doc);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestDeleteState()
        {
            var doc = new StateDocument();
            doc.activity = Support.activity;
            doc.agent = Support.agent;
            doc.id = "test";

            LRSResponse lrsRes = await lrs.DeleteState(doc);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestClearState()
        {
            LRSResponse lrsRes = await lrs.ClearState(Support.activity, Support.agent);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestRetrieveActivityProfileIds()
        {
            ProfileKeysLRSResponse lrsRes = await lrs.RetrieveActivityProfileIds(Support.activity);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestRetrieveActivityProfile()
        {
            ActivityProfileLRSResponse lrsRes = await lrs.RetrieveActivityProfile("test", Support.activity);
            Assert.True(lrsRes.success);
            Assert.IsType<ActivityProfileDocument>(lrsRes.content);
        }

        [Fact]
        public async Task TestSaveActivityProfile()
        {
            var doc = new ActivityProfileDocument();
            doc.activity = Support.activity;
            doc.id = "test";
            doc.content = System.Text.Encoding.UTF8.GetBytes("Test value");

            LRSResponse lrsRes = await lrs.SaveActivityProfile(doc);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestDeleteActivityProfile()
        {
            var doc = new ActivityProfileDocument();
            doc.activity = Support.activity;
            doc.id = "test";

            LRSResponse lrsRes = await lrs.DeleteActivityProfile(doc);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestRetrieveAgentProfileIds()
        {
            ProfileKeysLRSResponse lrsRes = await lrs.RetrieveAgentProfileIds(Support.agent);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestRetrieveAgentProfile()
        {
            AgentProfileLRSResponse lrsRes = await lrs.RetrieveAgentProfile("test", Support.agent);
            Assert.True(lrsRes.success);
            Assert.IsType<AgentProfileDocument>(lrsRes.content);
        }

        [Fact]
        public async Task TestSaveAgentProfile()
        {
            var doc = new AgentProfileDocument();
            doc.agent = Support.agent;
            doc.id = "test";
            doc.content = System.Text.Encoding.UTF8.GetBytes("Test value");

            LRSResponse lrsRes = await lrs.SaveAgentProfile(doc);
            Assert.True(lrsRes.success);
        }

        [Fact]
        public async Task TestDeleteAgentProfile()
        {
            var doc = new AgentProfileDocument();
            doc.agent = Support.agent;
            doc.id = "test";

            LRSResponse lrsRes = await lrs.DeleteAgentProfile(doc);
            Assert.True(lrsRes.success);
        }
    }
}
