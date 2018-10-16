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
    public class JobsListTest
    {
        [TestMethod]
        [DataRow("some-reference","author","open","1", "status")]
        public async Task GetJobsTest(string buyer_team__reference, string created_by = null, string status = null, string page = null, string order_by = null)
        {
            // arrange
            Dictionary<string, string> jobsRequestData = new Dictionary<string, string>
            {
                {"buyer_team__reference", buyer_team__reference },
                {"created_by", created_by },
                {"status", status },
                {"page", page },
                {"order_by", order_by }
            };

            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v2/jobs.json", "GET", jobsRequestData)).Returns(Task.FromResult(TestUtils.GetJobsListJsonString()));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            var jobs = await upwork.GetJobs(buyer_team__reference, created_by, status, null, null, page, order_by);

            // assert
            jobs.Count.Should().Be(1);
        }
    }
}
