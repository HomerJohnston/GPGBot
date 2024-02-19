using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.ContinuousIntegration.Interface
{
    public interface IContinuousIntegrationSystem
    {
        Task<bool> StartJob(string jobName, string changeID, bool buildCode, bool buildWwise);
    }
}
