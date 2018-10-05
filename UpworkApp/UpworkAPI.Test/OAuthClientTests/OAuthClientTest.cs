using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UpworkAPI.Test;

namespace UpworkAPI.OAuthClientTests.Test
{
    [TestClass]
    public class OAuthClientTest
    {

        #region OAuthClient Constructor Tests
        [TestMethod]
        public void OAuthClientConstructorTest()
        {
            // arrange
            OAuthConfig config = TestUtils.GetOauthConfig();

            // act
            OAuthClient client = new OAuthClient(config);

            // assert
            client.Should().NotBeNull();
        }

        [TestMethod]
        public void OAuthClientConstructorExceptionTest()
        {
            // arrange
            OAuthConfig config = null;

            // assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new OAuthClient(config));
        }
        #endregion

        #region Generate Signature Test
        [TestMethod]
        [DataRow("https://www.testurl.com", "POST")]
        [DataRow("https://www.testurl.com", "GET")]
        public void GenerateSignatureTest(string url, string method)
        {
            // arrange
            OAuthClient client = TestUtils.GetOAuthClient();
            Dictionary<string, string> data = new Dictionary<string, string> { { "first_param_key", "firts_param_value" }, { "second_param_key", "second_param_value" } };

            //act
            string signature = client.GenerateSignature(url, method, data);

            // assert
            signature.Should().NotBeNullOrEmpty();
            signature.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        [DataRow("https://www.testurl.com", "POST")]
        [DataRow("https://www.testurl.com", "GET")]
        public void GenerateSignatureExceptionTest(string url, string method)
        {
            // arrange
            OAuthClient client = TestUtils.GetOAuthClient();
            Dictionary<string, string> data = null;

            // assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => client.GenerateSignature(url, method, data));
        }
        #endregion

        #region Generating OAuth Header Tests
        [TestMethod]
        public void GenerateOAuthHeaderTest()
        {
            // arrange
            OAuthClient client = TestUtils.GetOAuthClient();
            Dictionary<string, string> data = new Dictionary<string, string> { { "first_oauth_param_key", "firts_oauth_param_value" }, { "second_oauth_param_key", "second_oauth_param_value" } };

            // act 
            string header = client.GenerateOAuthHeader(data);

            // assert
            header.Should().NotBeNullOrEmpty();
            header.Should().NotBeNullOrWhiteSpace();
            header.Should().StartWith("OAuth");
        }

        [TestMethod]
        public void GenerateOAuthHeaderExceptionTest()
        {
            // arrange
            OAuthClient client = TestUtils.GetOAuthClient();
            Dictionary<string, string> data = null;

            // assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => client.GenerateOAuthHeader(data));
        }

        #endregion

        #region GenerateNonce Test
        [TestMethod]
        public void GenerateNonceTest()
        {
            // arrange
            OAuthClient client = TestUtils.GetOAuthClient();

            // act 
            string nonce = client.GenerateNonce();

            // assert
            nonce.Should().NotBeNullOrEmpty();
            nonce.Should().NotBeNullOrWhiteSpace();
        }
        #endregion

        //[TestMethod]
        //public void GetRequestTokensTest(string url)
        //{
        //    // arrange
        //    OAuthClient client = TestUtils.GetOAuthClient();
        //    Dictionary<string, string> data = new Dictionary<string, string> { { "first_param_key", "firts_param_value" }, { "second_param_key", "second_param_value" } };

        //    //act
        //    string signature = client.GenerateSignature(url, method, data);

        //    // assert
        //    signature.Should().NotBeNullOrEmpty();
        //    signature.Should().NotBeNullOrWhiteSpace();
        //}
    }
}
