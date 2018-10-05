using System;
using System.Collections.Generic;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using UpworkAPI.Interfaces;
using UpworkAPI.Models;
using FluentAssertions;

namespace UpworkAPI.Test.UpworkAPITests
{
    [TestClass]
    public class UserInfoTest
    {
        [TestMethod]
        public async Task GetUserInfoTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/auth/v1/info.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult(TestUtils.GetUserInfoJsonString()));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            UpworkUser user = await upwork.GetUserInfo();

            //assert
            user.AuthUser.Should().NotBeNull();
            user.AuthUser.FirstName.Should().NotBeNullOrEmpty();
            user.AuthUser.LastName.Should().NotBeNullOrEmpty();
            user.Info.Should().NotBeNull();
            user.Info.Ref.Should().NotBeNullOrEmpty();
            
        }

        [TestMethod]
        public void GetUserInfoExceptionTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/auth/v1/info.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult("abcd"));
            IUpwork upwork = new Upwork(clientMock.Object);

            // assert
            Func<Task> action = async () => { UpworkUser user = await upwork.GetUserInfo(); };
            action.Should().Throw<Exception>();

        }
    }
}
