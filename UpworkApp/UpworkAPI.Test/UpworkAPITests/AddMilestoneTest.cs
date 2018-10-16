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
    public class AddMilestoneTest
    {
        [TestMethod]
        [DataRow("contract-reference","Test description",50)]
        public async Task CreateMilestoneTest(string contract_reference, string milestone_description, int deposit_amount)
        {
            // arrange
            DateTime? dueDate = DateTime.Now.AddDays(2);

            Dictionary<string, string> newMilestoneRequestData = new Dictionary<string, string>
            {
                {"contract_reference", contract_reference },
                {"milestone_description", milestone_description },
                {"deposit_amount", deposit_amount.ToString() },
                {"due_date", dueDate?.ToString("MM-dd-yyyy") }
            };

            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v3/fp/milestones.json", "POST", newMilestoneRequestData)).Returns(Task.FromResult("{'id':'abcd98765321'}"));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            string newMilestoneId = await upwork.CreateMilestone(contract_reference, milestone_description, deposit_amount, dueDate);

            // assert
            newMilestoneId.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task CreateMilestoneExceptionTest()
        {
            // arrange

            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/hr/v3/fp/milestones.json", "POST", new Dictionary<string, string>())).Returns(Task.FromResult("abcd"));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            //string newMilestoneId = await upwork.CreateMilestone(contract_reference, milestone_description, deposit_amount, dueDate);

            // assert
            Func<Task> contractReferenceNullAction = async () => { string newMilestoneId = await upwork.CreateMilestone(null, "milestone_description", 20); };
            contractReferenceNullAction.Should().Throw<ArgumentNullException>();

            Func<Task> descriptionNullAction = async () => { string newMilestoneId = await upwork.CreateMilestone("contract_reference", null, 20); };
            descriptionNullAction.Should().Throw<ArgumentNullException>();

            Func<Task> amountAction = async () => { string newMilestoneId = await upwork.CreateMilestone("contract_reference", "milestone_description", 0); };
            amountAction.Should().Throw<ArgumentException>();

            Func<Task> fullAction = async () => { string newMilestoneId = await upwork.CreateMilestone("contract_reference", "milestone_description", 10); };
            fullAction.Should().Throw<Exception>();
        }
    }
}
