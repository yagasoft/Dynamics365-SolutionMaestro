namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class ImportParams : IoParamsBase
	{
		public bool? IsClean
		{
			get => isClean ?? false;
			set => isClean ??= value;
		}

		private bool? isClean;
	}
}
