#region Imports

using JsonKnownTypes;
using Newtonsoft.Json;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class ConfigRefParams : PipelineParams
	{
		public string File
		{
			get => file;
			set => file ??= value;
		}

		private string file;
	}
}
