using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPGBot
{
	public struct BuildRecord
	{
		public BuildRecord() { }

		public int ChangeID { get; set; } = -1;
		public string UserName { get; set; } = String.Empty;
		public string JobName { get; set; } = String.Empty;
	}
}
