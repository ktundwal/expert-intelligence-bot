using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UpworkAPI.Interfaces;
using UpworkAPI.Models;

namespace UpworkAPI.Test.UpworkAPITests
{
    [TestClass]
    public class UserTeamsTest
    {
        [TestMethod]
        public async Task GetUserTeamsTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v2/teams.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult(TestUtils.GetUserTeamsJsonString()));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            List<UserTeam> userTeams = await upwork.GetUserTeams();

            //assert
            userTeams.Should().NotBeNullOrEmpty();
            UserTeam team = userTeams.First();
            team.Should().NotBeNull();
            team.Reference.Should().NotBeNullOrEmpty();
            team.Id.Should().NotBeNullOrEmpty();
            team.Name.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void GetUserTeamsExceptionTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v2/teams.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult("abcd"));
            IUpwork upwork = new Upwork(clientMock.Object);

            //assert
            // assert
            Func<Task> action = async () => { List<UserTeam> jobInfo = await upwork.GetUserTeams(); };
            action.Should().Throw<Exception>();
        }
    }
}
