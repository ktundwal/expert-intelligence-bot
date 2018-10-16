using System;
using System.Collections.Generic;
using System.Text;
using UpworkAPI.Interfaces;

namespace UpworkAPI
{
    internal class MilestoneService : IMilestoneService
    {
        /// <summary>
        /// OAuthClient
        /// </summary>
        IOAuthClient _client;

        /// <summary>
        /// Initializes a new instance of the UpworkAPI.MilestoneService class with a specified IOAuthClient
        /// </summary>
        /// <param name="client">OAuthClient type instance</param>
        /// <exception cref="System.ArgumentNullException">Thrown when OAuthClient parameter is null.</exception>
        public MilestoneService(IOAuthClient client)
        {
            _client = client ?? throw new ArgumentNullException("client");
        }
    }
}
