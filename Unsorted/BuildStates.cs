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
        Running,
		Succeeded,
		Failed,
		Unstable,
		Aborted
	}
}
