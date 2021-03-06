﻿using BotAuth;
using BotAuth.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query;
using SharePointBot.Model;
using SharePointBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SharePointBot.Services
{
    public class SharePointService : ISharePointService
    {
        /// <summary>
        /// Get web by title.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="accessToken"></param>
        /// <returns>A BotSite representing the web if it exists, otherwise null.</returns>
        public async Task<List<BotSite>> SearchForWeb(string title, AuthResult auth, IBotContext context)
        {
            var retVal = new List<BotSite>();
            
            // We need to know the resource ID. This *should be* stored in bot state from when user logged in.
            string lastSiteCollectionUrl = null;
            if (!context.UserData.TryGetValue<string>(Constants.StateKeys.LastLoggedInSiteCollectionUrl, out lastSiteCollectionUrl))
            {
                throw new InvalidOperationException("Could not find current tenant URL in bot state.");
            }

            using (var clientContext = new ClientContext(lastSiteCollectionUrl))
            {
                clientContext.ExecutingWebRequest += (object sender, WebRequestEventArgs e) =>
                {
                    e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + auth.AccessToken;
                };


                KeywordQuery keywordQuery = new KeywordQuery(clientContext);
                keywordQuery.TrimDuplicates = true;
                keywordQuery.QueryText = $"{title}  (contentclass:STS_Web OR contentclass:STS_Site)";
                SearchExecutor searchExecutor = new SearchExecutor(clientContext);
                ClientResult<ResultTableCollection> results = searchExecutor.ExecuteQuery(keywordQuery);
                clientContext.ExecuteQuery();

                if (results.Value.Count > 0)
                {
                    if (results.Value[0].RowCount > 0)
                    {
                        foreach (var row in results.Value[0].ResultRows)
                        {
                            // TODO : find a more robust way to get URL. If can't find it, then don't include this search result.
                            if (row["SPWebUrl"] != null && !string.IsNullOrEmpty(row["SPWebUrl"].ToString()))
                            {
                                retVal.Add(new BotSite
                                {
                                    Alias = string.Empty,
                                    Id = Guid.Empty,
                                    Title = row["Title"].ToString(),
                                    Url = row["SPWebUrl"].ToString()
                                });
                            }
                        }
                    }
                }
            }

            return retVal;
        }
    }
}