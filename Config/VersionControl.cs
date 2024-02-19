﻿using PercivalBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PercivalBot.Config
{
	public class VersionControl
	{
		public EVersionControlSystem System { get; set; }
		public string? Address { get; set; }
		public string? User { get; set; }
		public string? Password { get; set; }
	}
}
