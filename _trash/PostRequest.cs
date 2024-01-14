namespace GPGBot
{
	public class PostRequest
	{
		int BuildID { get; set; } = -1;
		BuildStates BuildState { get; set; } = BuildStates.NULL;
        int ChangeID { get; set; } = -1;
        string UserName { get; set; } = string.Empty;
		string WorkspaceName { get; set; } = string.Empty;
	}
}
