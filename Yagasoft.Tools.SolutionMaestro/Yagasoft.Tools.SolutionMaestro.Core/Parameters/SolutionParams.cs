#region Imports

using System.Collections.Generic;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class SolutionParams
	{
		public GlobalParams Global { get; set; }
		public IEnumerable<PipelineParams> Pipeline { get; set; }
	}
}
