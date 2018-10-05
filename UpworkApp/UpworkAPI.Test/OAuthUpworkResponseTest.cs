using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UpworkAPI.Test
{
    [TestClass]
    public class OAuthUpworkResponseTest
    {
        [TestMethod]
        [DataRow("first_key=first_value&second_key=second_value","first_key","first_value")]
        [DataRow("first_key=first_value&second_key=second_value", "second_key", "second_value")]
        public void OAuthResponseTest(string response,string key, string value)
        {
            // arrange
            OAuthUpworkResponse responseResult = new OAuthUpworkResponse(response);

            // assert
            responseResult[key].Should().Be(value);
        }
    }
}
