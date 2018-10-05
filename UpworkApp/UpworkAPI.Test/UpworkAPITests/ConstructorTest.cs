using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UpworkAPI.Test.UpworkAPITests
{
    [TestClass]
    public class ConstructorTest
    {
        [TestMethod]
        public void UpworkConstructorTest()
        {
            // arrange
            OAuthClient client = TestUtils.GetOAuthClient();

            // act
            Upwork upworkAPI = new Upwork(client);

            //assert
            upworkAPI.Should().NotBeNull();
        }

        [TestMethod]
        public void UpworkConstructorExceptionTest()
        {
            // arrange
            OAuthClient client = null;

            // assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new Upwork(client));
        }
    }


}
