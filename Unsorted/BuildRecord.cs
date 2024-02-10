using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot
{
	public struct BuildRecord
	{
		public string jobName;
		public ulong buildID;
		//public string changeID;
		//public string user;

		public BuildRecord(string jobName, ulong buildID)//, string changeID, string user)
		{
			this.jobName = jobName;
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
