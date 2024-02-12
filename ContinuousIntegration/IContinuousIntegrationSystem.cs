using GPGBot.EmbedBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.ContinuousIntegration
{
    public interface IContinuousIntegrationSystem
    {
        Task<bool> StartJob(string jobName);
        Task<bool> StartJob(string jobName, string changeID);
    }
}
