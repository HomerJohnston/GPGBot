using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PercivalBot.Enums;
using PercivalBot.Structs;

namespace PercivalBot.ChatClients.Interface
{
    public interface IEmbedBuilder
    {
        string GetBuildURL(string buildConfigName, string buildID);

        string GetConsoleURL(string buildConfigName, string buildID);

        string GetChangesURL(string buildConfigName, string buildID);

        Embed ConstructBuildStatusEmbed(BuildStatusEmbedData embedData);//int buildID, string embedTitle, string embedIconURL, Color embedColor, string embedDescription, int changeID, string userName, string buildConfigName);
    }
}
