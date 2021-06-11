namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class IoParamsBase : PipelineParams
	{
		public string Names { get; set; }

		public bool? IsPublish
		{
			get => isPublish ?? true;
			set => isPublish ??= value;
		}

		private bool? isPublish;
	}
}
