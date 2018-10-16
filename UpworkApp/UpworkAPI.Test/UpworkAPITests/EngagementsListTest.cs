using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UpworkAPI.Interfaces;

namespace UpworkAPI.Test.UpworkAPITests
{
    [TestClass]
    public class EngagementsListTest
    {
        [TestMethod]
        [DataRow("team-reference",0,"provider-reference","profile-key","gob-reference","agency-team-reference","open","1","status")]
        public async Task GetEngagementsTest(string buyerTeamReference = null, int includeSubTeams = 0, string providerReference = null, string profileKey = null, string jobReference = null, string agencyTeamReference = null, string status = null, string page = null, string orderBy = null)
        {
            // arrange
            DateTime? createdTimeFrom = DateTime.Now;
            DateTime? createdTimeTo = DateTime.Now;

            Dictionary<string, string> engagementsRequestData = new Dictionary<string, string>
            {
                {"include_sub_teams", includeSubTeams.ToString() },
                {"buyer_team__reference", buyerTeamReference },
                {"provider__reference", providerReference },
                {"profile_key", profileKey },
                {"job__reference", jobReference },
                {"agency_team__reference", agencyTeamReference },
                {"status", status },
                {"created_time_from", createdTimeFrom?.ToString("yyyy-MM-ddThh:mm:ss") },
                {"created_time_to", createdTimeTo?.ToString("yyyy-MM-ddThh:mm:ss") },
                {"page", page },
                {"order_by", orderBy },
            };

            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v2/engagements.json", "GET", engagementsRequestData)).Returns(Task.FromResult(TestUtils.GetEngagementsListJsonString()));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            var engagements = await upwork.GetEngagements(buyerTeamReference, includeSubTeams, providerReference ,profileKey, jobReference, agencyTeamReference, status, createdTimeFrom, createdTimeTo, page, orderBy);

            // assert
            engagements.Count.Should().Be(1);
        }
    }
}
