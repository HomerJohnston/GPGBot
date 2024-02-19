using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Structs
{
    public struct BuildRecord
    {
        public string jobName;
        public string buildNumber;
        public string buildID;

        public BuildRecord(string jobName, string buildNumber, string buildID)//, string changeID, string user)
        {
            this.jobName = jobName;
            this.buildNumber = buildNumber;
            this.buildID = buildID;
            //this.changeID = changeID;
            //this.user = user;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(jobName, buildID);
        }

        public bool Equals(BuildRecord other)
        {
            return jobName == other.jobName && buildID == other.buildID;
        }
    }
}
