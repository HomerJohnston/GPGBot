using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot
{
	public enum BuildStates
    {
        NULL,
        Started,
		Succeeded,
		Failed,
		Unstable,
		Aborted
	}
}
