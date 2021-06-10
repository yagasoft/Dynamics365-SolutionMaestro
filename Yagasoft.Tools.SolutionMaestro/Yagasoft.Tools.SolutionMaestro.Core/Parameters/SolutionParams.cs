#region Imports

using System.Collections.Generic;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class SolutionParams
	{
		public string Names { get; set; }
		public bool IsOverwrite { get; set; }
		public bool IsRetry { get; set; }
		public bool IsPublish { get; set; } = true;
		public string Path { get; set; }
	}
}
