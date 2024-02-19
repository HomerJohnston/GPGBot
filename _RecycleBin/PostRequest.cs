using PercivalBot.Enums;

namespace PercivalBot
{
    public class PostRequest
	{
		int BuildID { get; set; } = -1;
		EBuildStates BuildState { get; set; } = EBuildStates.NULL;
        int ChangeID { get; set; } = -1;
        string UserName { get; set; } = string.Empty;
		string WorkspaceName { get; set; } = string.Empty;
	}
}
