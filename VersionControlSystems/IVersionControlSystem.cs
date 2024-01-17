using GPGBot.CommandHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot.VersionControlSystems
{
	public interface IVersionControlSystem
	{
		string? GetStream(string? change, string? client);

		string? GetCommitDescription(string? change);
	}
}
