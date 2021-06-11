#region Imports

using JsonKnownTypes;
using Newtonsoft.Json;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	[JsonConverter(typeof(JsonKnownTypesConverter<PipelineParams>))]
	[JsonDiscriminator(Name = "operation")]
	[JsonKnownType(typeof(ExportParams), "export")]
	[JsonKnownType(typeof(ImportParams), "import")]
	public class PipelineParams : ParamsBase
	{ }
}
