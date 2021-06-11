#region Imports



#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class ExportParams : IoParamsBase
	{
		public bool? IsManaged
		{
			get => isManaged ?? false;
			set => isManaged ??= value;
		}

		private bool? isManaged;
	}
}
