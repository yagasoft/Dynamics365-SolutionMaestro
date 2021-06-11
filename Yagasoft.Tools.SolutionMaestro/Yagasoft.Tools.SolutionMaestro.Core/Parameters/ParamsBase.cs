#region Imports



#endregion

using Yagasoft.Libraries.Common;

namespace Yagasoft.Tools.SolutionMaestro.Core.Parameters
{
	public class ParamsBase
	{
		public bool? IsDateFile
		{
			get => isDateFile ?? false;
			set => isDateFile ??= value;
		}

		public bool? IsOverwrite
		{
			get => isOverwrite ?? false;
			set => isOverwrite ??= value;
		}

		public bool? IsRetry
		{
			get => isRetry ?? false;
			set => isRetry ??= value;
		}

		public string Path
		{
			get => path.IsFilled() ? path : ".";
			set => path ??= value;
		}

		private bool? isDateFile;
		private bool? isOverwrite;
		private bool? isRetry;
		private string path;
	}
}
