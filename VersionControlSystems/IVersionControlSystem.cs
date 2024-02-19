using GPGBot.CommandHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.VersionControlSystems
{
	public enum EChangeFileAction
	{
		Add,
		Edit,
		Delete,
	}

	public struct FileActionSpec
	{
		public string file;
		public string action;
	}

	public interface IVersionControlSystem
	{
		string? GetStream(string? change, string? client);

		string? GetCommitDescription(string? change);

		public void GetRequiredActionsBasedOnChanges(string? change, out bool buildCode, out bool buildWwise);
	}
}
