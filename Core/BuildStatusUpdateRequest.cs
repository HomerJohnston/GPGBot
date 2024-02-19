using PercivalBot.Enums;
using Perforce.P4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PercivalBot.Core
{
	public class BuildStatusUpdateRequest
	{
		public string ChangeID { get; }
		public string JobName { get; }
		public string BuildNumber { get; }
		public string BuildID { get; }
		public EBuildStatus Status { get; }

		public BuildStatusUpdateRequest(string changeID, string jobName, string buildNumber, string buildID, string buildStatusParam)
		{
			ChangeID = changeID;
			JobName = jobName;
			BuildNumber = buildNumber;
			BuildID = buildID;

			EBuildStatus parsedBuildStatus;

			if (!Enum.TryParse(buildStatusParam, true, out parsedBuildStatus))
			{
				Status = EBuildStatus.NULL;
			}
			else 
			{
				Status = parsedBuildStatus;
			}
		}

		// --------------------------------------
		public bool IsValid(out string error)
		{
			bool valid = true;
			error = string.Empty;

			if (ChangeID == string.Empty)
			{
				valid = false;
				error = string.Join(", ", error, "ChangeID unset");
			}

			if (JobName == string.Empty)
			{
				valid = false;
				error = string.Join(", ", error, "JobName unset");
			}

			if (BuildNumber == string.Empty)
			{
				valid = false;
				error = string.Join(", ", error, "BuildNumber unset");
			}

			if (BuildID == string.Empty)
			{
				valid = false;
				error = string.Join(", ", error, "BuildID unset");
			}

			if (Status == EBuildStatus.NULL)
			{
				valid = false;
				error = String.Join(", ", error, "BuildStatus unset");
			}

			return valid;
		}

		// --------------------------------------
		public override string ToString()
		{
			return ($"ChangeID: {NoneOr(ChangeID)}, JobName: {NoneOr(JobName)}, BuildNumber: {NoneOr(BuildNumber)}, BuildID {NoneOr(BuildID)}, BuildStatus {Status}");
		}

		// --------------------------------------
		private string NoneOr(string s)
		{
			return s == string.Empty ? "NONE" : s;
		}
	}
}
