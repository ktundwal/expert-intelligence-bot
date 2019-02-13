// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using com.microsoft.ExpertConnect.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using UserState = Microsoft.Bot.Builder.UserState;

namespace com.microsoft.ExpertConnect
{
    public class BotAccessors
    {
        // The property accessor keys to use.
        public const string UserInfoAccessorName = "ExpertConnect.UserInfo";
        public const string DialogStateAccessorName = "ExpertConnect.DialogState";

        /// <summary>
        /// Initializes a new instance of the <see cref="BotAccessors"/> class.
        /// Contains the <see cref="ConversationState"/> and associated <see cref="IStatePropertyAccessor{T}"/>.
        /// </summary>
        /// <param name="conversationState">The state object that stores the counter.</param>
        public BotAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }


        /// <summary>Gets or sets the state property accessor for the user information we're tracking.</summary>
        /// <value>Accessor for user information.</value>
        public IStatePropertyAccessor<UserInfo> UserInfoAccessor { get; set; }

        /// <summary>Gets or sets the state property accessor for the dialog state.</summary>
        /// <value>Accessor for the dialog state.</value>
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        /// <summary>Gets the conversation state for the bot.</summary>
        /// <value>The conversation state for the bot.</value>
        public ConversationState ConversationState { get; }

        /// <summary>Gets the user state for the bot.</summary>
        /// <value>The user state for the bot.</value>
        public UserState UserState { get; }
    }
}
