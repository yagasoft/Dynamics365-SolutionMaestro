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
using Yagasoft.Tools.SolutionMaestro.Core.IO.Models;
using Yagasoft.Tools.SolutionMaestro.Core.Parameters;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.IO
{
	public class ExportSolution
	{
		private readonly IOrganizationService service;
		private readonly CrmLog log;

		private ExportParams exportParams;

		private bool isPublished;

		public ExportSolution(IOrganizationService serviceParam, CrmLog logParam)
		{
			service = serviceParam;
			log = logParam;
		}

		public void ExportSolutions(ExportParams exportParamsParam)
		{
			exportParamsParam.Require(nameof(exportParamsParam));

			exportParams = exportParamsParam;

			var solutionInfo = RetrieveSolutionInformation().ToArray();

			foreach (var solutionName in exportParams.Names.Split(';'))
			{
				ExportedSolution exportedSolution;
				var retry = 1;

				do
				{
					try
					{
						exportedSolution = RetrieveSolution(solutionName, solutionInfo);
						break;
					}
					catch (Exception)
					{
						if (retry-- <= 0)
						{
							throw;
						}
					}
				}
				while (true);

				if (exportedSolution != null)
				{
					SaveSolutionFile(exportedSolution);
				}
			}
		}

		private ExportedSolution RetrieveSolution(string solutionName, IEnumerable<Entity> allSolutionInfoParam)
		{
			solutionName.RequireFilled(nameof(solutionName));
			allSolutionInfoParam.Require(nameof(allSolutionInfoParam));

			var allSolutionInfo = allSolutionInfoParam.ToArray();

			var solutionInfo = allSolutionInfo
				.Where(s => Regex.IsMatch(s.GetAttributeValue<string>(Solution.Name), solutionName)
					|| Regex.IsMatch(s.GetAttributeValue<string>(Solution.DisplayName_FriendlyName), solutionName)
					|| (s.GetAttributeValue<EntityReference>(Solution.ParentSolution)?.Name.IsFilled() == true
						&& Regex.IsMatch(s.GetAttributeValue<EntityReference>(Solution.ParentSolution).Name, solutionName))
					|| (s.GetAttributeValue<string>(Solution.ParentSolutionUniqueName)?.IsFilled() == true
						&& Regex.IsMatch(s.GetAttributeValue<string>(Solution.ParentSolutionUniqueName), solutionName)))
				.OrderByDescending(s => new Version(s.GetAttributeValue<string>(Solution.Version))).FirstOrDefault();

			if (solutionInfo == null)
			{
				throw new ToolException($"Couldn't find a solution matching pattern: {solutionName}.");
			}

			log.Log(
				$@"Found solution matching '{solutionName}':
{solutionInfo.GetAttributeValue<string>(Solution.DisplayName_FriendlyName)}
{solutionInfo.GetAttributeValue<string>(Solution.Name)}
v{solutionInfo.GetAttributeValue<string>(Solution.Version)}");

			var exportedSolution =
				new ExportedSolution
				{
					SolutionName = solutionInfo.GetAttributeValue<string>(Solution.ParentSolutionUniqueName)
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
					Managed = exportParams.IsManaged,
					SolutionName = solutionInfo.GetAttributeValue<string>(Solution.Name)
				};

			if (!isPublished && exportParams.IsPublish)
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
				solution[Solution.ParentSolutionUniqueName] = solutions
					.FirstOrDefault(ps => ps.Id == solution.GetAttributeValue<EntityReference>(Solution.ParentSolution).Id)?
					.GetAttributeValue<string>(Solution.Name);
			}

			return solutions;
		}

		private void SaveSolutionFile(ExportedSolution solution)
		{
			EnsureFolderExists();

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

				if (!exportParams.IsOverwrite)
				{
					return false;
				}
			}

			return true;
		}

		private void EnsureFolderExists()
		{
			var folder = GetFolderPath();

			if (!Directory.Exists(folder))
			{
				log.Log($"Creating folder '{folder}' ...");
				Directory.CreateDirectory(folder);
			}
		}

		private string GetFolderPath()
		{
			return exportParams.Path.IsFilled() ? exportParams.Path : ".";
		}

		private string GetFilePath(ExportedSolution solution)
		{
			var folder = GetFolderPath();
			var fileName = $"{solution.SolutionName}_{solution.Version}"
				+ $"{(exportParams.IsDateFile ? $"_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}" : "")}.zip";
			return Path.Combine(folder, fileName);
		}
	}
}
