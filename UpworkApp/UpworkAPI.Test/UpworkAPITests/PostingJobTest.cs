using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UpworkAPI.Interfaces;
using UpworkAPI.Models;

namespace UpworkAPI.Test.UpworkAPITests
{
    [TestClass]
    public class PostingJobTest
    {
        [TestMethod]
        [DataRow("Job title", "buyer_team_reference", "Job description", "Category title", "Subcategory title", "fixed-price", "private", "php,ajax", true, 50, 10, "all")]
        [DataRow("Job title", "buyer_team_reference", "Job description", "Category title", "Subcategory title", "fixed-price", "private", null, false, null, null, "all")]
        public async Task PostJobTest(string title, string buyerTeamReference, string description, string category, string subCategory, string jobType, string visibility, string skills, bool addDate, int? budget = null, int? duration = null, string contractorType = null)
        {
            // arrange
            UpworkJob newJob = new UpworkJob(title, buyerTeamReference, description, category, subCategory, jobType, visibility, skills, addDate ? DateTime.Now : (DateTime?)null, budget, duration, contractorType);
            var dictJob = newJob.ToUpworkDictionary();

            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v2/jobs.json", "POST", dictJob)).Returns(Task.FromResult(TestUtils.GetPostedJobJsonString()));

            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            JobInfo newJobInfo = await upwork.PostJob(newJob);

            //assert
            newJobInfo.Should().NotBeNull();
        }

        [TestMethod]
        public void PostJobExceptionTest()
        {
            // arrange
            UpworkJob newJob = new UpworkJob("Job title", "buyer_team_reference", "Job description", "Category title", "Subcategory title", "fixed-price", "private", "php,ajax", null, 50, 10, "all");
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v2/jobs.json", "POST", newJob.ToUpworkDictionary())).Returns(Task.FromResult("abcd"));
            IUpwork upwork = new Upwork(clientMock.Object);

            // assert
            Func<Task> action = async () => { JobInfo jobInfo = await upwork.PostJob(newJob); };
            action.Should().Throw<Exception>();
        }
    }
}
