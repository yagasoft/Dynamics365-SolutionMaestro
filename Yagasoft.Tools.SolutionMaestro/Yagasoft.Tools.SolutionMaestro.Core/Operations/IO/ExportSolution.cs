#region Imports

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Tools.Common.Exceptions;
using Yagasoft.Tools.Common.Helpers;
using Yagasoft.Tools.SolutionMaestro.Core.Operations.IO.Models;
using Yagasoft.Tools.SolutionMaestro.Core.Parameters;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Operations.IO
{
	[Log]
	public class ExportSolution : OperationBase
	{
		private readonly IOrganizationService service;
		private readonly CrmLog log;

		private ExportParams config;

		private bool isPublished;

		public ExportSolution(IOrganizationService serviceParam, CrmLog logParam)
		{
			service = serviceParam;
			log = logParam;
		}

		public void Export(ExportParams exportParams)
		{
			exportParams.Require(nameof(exportParams));

			config = exportParams;

			log.Log($"Processing export config ...\r\n{ConfigHelpers.Serialise(config)}");

			var solutionInfo = RetrieveSolutionInformation().ToArray();

			foreach (var solutionName in config.Names.Split(';'))
			{
				ExportedSolution[] exportedSolutions;
				var retry = config.IsRetry == true ? 1 : 0;

				do
				{
					try
					{
						exportedSolutions = RetrieveSolution(solutionName, solutionInfo).ToArray();
						break;
					}
					catch
					{
						if (retry-- <= 0)
						{
							throw;
						}
	
						log.LogWarning("Retrying ...");
					}
				}
				while (true);

				foreach (var exportedSolution in exportedSolutions)
				{
					SaveSolutionFile(exportedSolution);
				}
			}
		}

		private IEnumerable<ExportedSolution> RetrieveSolution(string solutionName, IEnumerable<Entity> allSolutionInfoParam)
		{
			solutionName.RequireFilled(nameof(solutionName));
			allSolutionInfoParam.Require(nameof(allSolutionInfoParam));

			var allSolutionInfo = allSolutionInfoParam.ToArray();

			var solutionsInfo = allSolutionInfo
				.Where(s => Regex.IsMatch(s.GetAttributeValue<string>(Solution.Name), solutionName)
					|| Regex.IsMatch(s.GetAttributeValue<string>(Solution.DisplayName_FriendlyName), solutionName)
					|| (s.GetAttributeValue<EntityReference>(Solution.ParentSolution)?.Name.IsFilled() == true
						&& Regex.IsMatch(s.GetAttributeValue<EntityReference>(Solution.ParentSolution).Name, solutionName))
					|| (s.GetAttributeValue<string>(Models.Solution.ParentSolutionUniqueName)?.IsFilled() == true
						&& Regex.IsMatch(s.GetAttributeValue<string>(Models.Solution.ParentSolutionUniqueName), solutionName)))
				.OrderByDescending(s => new Version(s.GetAttributeValue<string>(Solution.Version))).ToArray();

			if (!solutionsInfo.Any())
			{
				throw new ToolException($"Couldn't find a solution matching pattern: {solutionName}.");
			}

			return
				solutionsInfo.Select(
					solutionInfo =>
					{
						log.Log(
							$@"Found solution matching '{solutionName}':
{solutionInfo.GetAttributeValue<string>(Solution.DisplayName_FriendlyName)}
{solutionInfo.GetAttributeValue<string>(Solution.Name)}
v{solutionInfo.GetAttributeValue<string>(Solution.Version)}");

						var exportedSolution =
							new ExportedSolution
							{
								SolutionName = solutionInfo.GetAttributeValue<string>(Models.Solution.ParentSolutionUniqueName)
									?? solutionInfo.GetAttributeValue<string>(Solution.Name),
								Version = solutionInfo.GetAttributeValue<string>(Solution.Version)
							};

						if (!IsAllowedWrite(exportedSolution))
						{
							log.LogWarning("Skipping.");
							return null;
						}

						var request =
							new ExportSolutionRequest
							{
								Managed = config.IsManaged == true,
								SolutionName = solutionInfo.GetAttributeValue<string>(Solution.Name)
							};

						if (!isPublished && config.IsPublish == true)
						{
							log.Log($"Publishing customisations ...");
							service.Execute(new PublishAllXmlRequest());
							isPublished = true;
							log.Log($"[Finished] Publishing customisations.");
						}

						log.Log($"Exporting ...");
						var response = (ExportSolutionResponse)service.Execute(request);
						log.Log($"[Finished] Exported.");

						exportedSolution.Data = response.ExportSolutionFile;

						return exportedSolution;
					})
					.Where(s => s != null);
		}

		private IEnumerable<Entity> RetrieveSolutionInformation()
		{
			var query =
				new QueryExpression
				{
					EntityName = Solution.EntityLogicalName,
					ColumnSet = new ColumnSet(
						Solution.Name,
						Solution.DisplayName_FriendlyName,
						Solution.Version,
						Solution.ParentSolution),
					Criteria = new FilterExpression(),
					NoLock = true
				};
			query.Criteria.AddCondition(Solution.PackageType, ConditionOperator.NotEqual, true);
			query.Criteria.AddCondition(Solution.IsVisibleOutsidePlatform, ConditionOperator.Equal, true);

			log.Log($"Retrieving solution information ...");
			var solutions = service.RetrieveMultiple(query).Entities.ToArray();
			log.Log($"Found: {solutions.Length} solutions.");

			foreach (var solution in solutions.Where(s => s.GetAttributeValue<EntityReference>(Solution.ParentSolution) != null))
			{
				solution[Models.Solution.ParentSolutionUniqueName] = solutions
					.FirstOrDefault(ps => ps.Id == solution.GetAttributeValue<EntityReference>(Solution.ParentSolution).Id)?
					.GetAttributeValue<string>(Solution.Name);
			}

			return solutions;
		}

		private void SaveSolutionFile(ExportedSolution solution)
		{
			FileHelpers.EnsureFolderExists(config.Path);

			if (!IsAllowedWrite(solution))
			{
				log.LogWarning("Skipping.");
				return;
			}

			var path = GetFilePath(solution);
			log.Log($"Writing file content '{path}' ...");
			File.WriteAllBytes(path, solution.Data);
			log.Log($"[Finished] File saved.");
		}

		private bool IsAllowedWrite(ExportedSolution solution)
		{
			var filePath = GetFilePath(solution);

			if (File.Exists(filePath))
			{
				log.LogWarning($"File already exists ({filePath}).");

				if (!config.IsOverwrite == true)
				{
					return false;
				}
			}

			return true;
		}

		private string GetFilePath(ExportedSolution solution)
		{
			var folder = config.Path;
			var fileName = $"{solution.SolutionName}_{solution.Version}"
				+ $"{(config.IsDateFile == true ? $"_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}" : "")}.zip";
			return Path.Combine(folder, fileName);
		}
	}
}
