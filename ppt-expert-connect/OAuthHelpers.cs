// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace com.microsoft.ExpertConnect
{
    // This class calls the Microsoft Graph API. The following OAuth scopes are used:
    // 'OpenId' 'email' 'Mail.Send.Shared' 'Mail.Read' 'profile' 'User.Read' 'User.ReadBasic.All'
    // for more information about scopes see:
    // https://developer.microsoft.com/en-us/graph/docs/concepts/permissions_reference
    public static class OAuthHelpers 
    {
        public static string LoginPromptDialogId = "loginPrompt";

        // Prompts the user to log in using the OAuth provider specified by the connection name.
        public static OAuthPrompt Prompt(string connectionName)
        {
            return new OAuthPrompt(
                LoginPromptDialogId,
                new OAuthPromptSettings
                {
                    ConnectionName = connectionName,
                    Text = "Please login and provide consent to write files onto your OneDrive",
                    Title = "Login",
                    Timeout = 300000, // User has 5 minutes to login
//                    Timeout = 300
                });
        }
    }
}
