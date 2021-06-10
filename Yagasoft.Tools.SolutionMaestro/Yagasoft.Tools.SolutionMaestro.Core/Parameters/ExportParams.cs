#region Imports

using System.Collections.Generic;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class ExportParams : SolutionParams
	{
		public bool IsDateFile { get; set; }
		public bool IsManaged { get; set; }
	}
}
