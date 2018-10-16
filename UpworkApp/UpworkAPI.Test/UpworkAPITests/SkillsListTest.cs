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
    public class SkillsListTest
    {
        [TestMethod]
        public async Task GetSkillsTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/profiles/v1/metadata/skills.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult("{'skills':['php','c#']}"));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            List<string> skills = await upwork.GetSkills();

            //assert
            skills.Should().NotBeNull();
            skills.Count.Should().Be(2);
            skills.Should().Contain("php");
            skills.Should().Contain("c#");
        }

        [TestMethod]
        public async Task GetSkillsExceptionTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/profiles/v1/metadata/skills.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult("abcd"));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            

            //assert
            Func<Task> action = async () => { List<string> skills = await upwork.GetSkills(); };
            action.Should().Throw<Exception>();
        }
    }
}
