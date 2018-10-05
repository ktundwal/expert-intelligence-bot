using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using UpworkAPI.Interfaces;
using UpworkAPI.Models;

namespace UpworkAPI.Test.UpworkAPITests
{
    [TestClass]
    public class CategoriesTest
    {
        [TestMethod]
        public async Task GetCategoriesTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/profiles/v2/metadata/categories.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult(TestUtils.GetCategoriesJsonString()));
            IUpwork upwork = new Upwork(clientMock.Object);

            // act
            List<Category> categories = await upwork.GetCategories();

            //assert
            categories.Should().NotBeNullOrEmpty();
            Category category = categories.First();
            category.Should().NotBeNull();
            category.Id.Should().NotBeNullOrEmpty();
            category.Title.Should().NotBeNullOrEmpty();
            category.Topics.Should().NotBeNullOrEmpty();

            Category subCategory = category.Topics.First();
            subCategory.Id.Should().NotBeNullOrEmpty();
            subCategory.Title.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void GetCategoriesExceptionTest()
        {
            // arrange
            var clientMock = new Mock<IOAuthClient>();
            clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/profiles/v2/metadata/categories.json", "GET", new Dictionary<string, string>())).Returns(Task.FromResult("abcd"));
            IUpwork upwork = new Upwork(clientMock.Object);

            // assert
            Func<Task> action = async () => { List<Category> categories = await upwork.GetCategories(); };
            action.Should().Throw<Exception>();
        }
    }
}
