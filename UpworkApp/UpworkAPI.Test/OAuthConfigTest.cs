using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UpworkAPI.Test
{
    [TestClass]
    public class OAuthConfigTest
    {
        [TestMethod]
        [DataRow("test_consumer_key", "test_secret_Key","oauth-token","oauth-token-secret")]
        [DataRow("test_consumer_key", "test_secret_Key","", "oauth-token-secret")]
        [DataRow("test_consumer_key", "test_secret_Key", null, "oauth-token-secret")]
        [DataRow("test_consumer_key", "test_secret_Key", "oauth-token", "")]
        [DataRow("test_consumer_key", "test_secret_Key", "oauth-token", null)]
        public void OAuthConfigConstructorTest(string consumerKey, string consumerSecret, string oAuthToken, string oAuthTokenSecret)
        {
            // act
            OAuthConfig config = new OAuthConfig(consumerKey, consumerSecret, oAuthToken, oAuthTokenSecret);

            // assert
            config.ConsumerKey.Should().Be(consumerKey);
            config.ConsumerSecret.Should().Be(consumerSecret);
            config.OAuthToken.Should().Be(oAuthToken);
            config.OAuthTokenSecret.Should().Be(oAuthTokenSecret);
        }

        [TestMethod]
        [DataRow(null, "test_secret_Key", "oauth-token", "oauth-token-secret")]
        [DataRow("test_consumer_key", null, "oauth-token", "oauth-token-secret")]
        public void OAuthConfigConstructorExceptionTest(string consumerKey, string consumerSecret, string oAuthToken, string oAuthTokenSecret)
        {
            // assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new OAuthConfig(consumerKey, consumerSecret, oAuthToken, oAuthTokenSecret));
        }
    }
}
