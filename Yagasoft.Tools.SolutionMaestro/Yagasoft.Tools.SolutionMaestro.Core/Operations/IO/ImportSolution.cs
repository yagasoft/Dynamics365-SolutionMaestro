#region Imports

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Tools.Common.Exceptions;
using Yagasoft.Tools.Common.Helpers;
using Yagasoft.Tools.SolutionMaestro.Core.Parameters;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Core.Operations.IO
{
	[Log]
	public class ImportSolution : OperationBase
	{
		private readonly IOrganizationService service;
		private readonly CrmLog log;

		private ImportParams config;
		private bool isImportedSolution;

		public ImportSolution(IOrganizationService serviceParam, CrmLog logParam)
		{
			service = serviceParam;
			log = logParam;
		}

		public void ImportSolutions(ImportParams importParams)
		{
			importParams.Require(nameof(importParams));

			config = importParams;

			log.Log($"Processing import config ...\r\n{ConfigHelpers.Serialise(importParams)}");
			FileHelpers.EnsureFolderExists(config.Path);
			
			foreach (var solutionName in config.Names.Split(';'))
			{
				var retry = config.IsRetry == true ? 1 : 0;

				do
				{
					try
					{
						log.Log($"Processing solution '{solutionName}' ...");

						var solutions = LoadSolutions(solutionName);

						foreach (var solution in solutions)
						{
							retry = config.IsRetry == true ? 1 : 0;

							try
							{
								var isUpdated = IsSolutionUpdated(solution);

								if (!isUpdated)
								{
									log.LogWarning("Identical solution versions.");
								}

								if (config.IsOverwrite == true || isUpdated)
								{
									var isImported = Import(solution);

									if (!isImported)
									{
										throw new ToolException("Failed to import solution.");
									}
								}
								else
								{
									log.LogWarning("Skipping.");
								}

								log.Log($"[Finished] Processing solution '{solution.Name}'.");

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
			}
		}

		public void Finalise()
		{
			if (config.IsPublish == true && isImportedSolution)
			{
				Publish();
			}
		}

		[NoLog]
		private bool IsSolutionUpdated(SolutionInfo solution)
		{
			solution.Require(nameof(solution));

			log.Log("Checking existing solution version ...");

			var versionString = RetrieveSolutionVersion(solution.Name);

			if (string.IsNullOrWhiteSpace(versionString))
			{
				return true;
			}

			var existingVersion = new Version(versionString);
			var givenVersion = new Version(solution.Version);
			var isUpdated = givenVersion > existingVersion;

			if (isUpdated)
			{
				log.Log("Solution updated!");
			}

			return isUpdated;
		}

		private string RetrieveSolutionVersion(string solutionName)
		{
			solutionName.RequireNotEmpty(nameof(solutionName));

			var query =
				new QueryExpression
				{
					EntityName = Solution.EntityLogicalName,
					ColumnSet = new ColumnSet(Solution.Version),
					Criteria = new FilterExpression()
				};
			query.Criteria.AddCondition(Solution.Name, ConditionOperator.Equal, solutionName);

			log.Log($"Retrieving solution version for solution '{solutionName}'...");
			var solution = service.RetrieveMultiple(query).Entities.FirstOrDefault();
			var version = solution?.GetAttributeValue<string>(Solution.Version);
			log.Log($"Server solution version: {version}.");

			return version;
		}

		private bool Import(SolutionInfo solution)
		{
			solution.Require(nameof(solution));

			var importJobId = Guid.NewGuid();

			var request =
				new ExecuteAsyncRequest
				{
					Request =
						new ImportSolutionRequest
						{
							CustomizationFile = solution.Data,
							ConvertToManaged = solution.IsManaged,
							OverwriteUnmanagedCustomizations = config.IsClean == true,
							PublishWorkflows = true,
							SkipProductUpdateDependencies = true,
							ImportJobId = importJobId
						}
				};

			log.Log($"Importing solution '{solution.Name}' ...");

			service.Execute(request);

			MonitorJobProgress(importJobId);

			var job = service.Retrieve("importjob", importJobId, new ColumnSet(ImportJob.Progress, ImportJob.Data));

			var importXmlLog = job.GetAttributeValue<string>(ImportJob.Data);

			if (importXmlLog.IsNotEmpty())
			{
				var isFailed = ProcessErrorXml(importXmlLog);

				if (isFailed)
				{
					return false;
				}
			}

			log.Log($"Imported!");
			isImportedSolution = true;

			return true;
		}

		private void MonitorJobProgress(Guid importJobId)
		{
			var progress = 0d;
			Entity job = null;

			do
			{
				Thread.Sleep(5000);

				try
				{
					job = service.Retrieve("importjob", importJobId,
						new ColumnSet(ImportJob.Progress, ImportJob.CompletedOn));

					var currentProgress = job.GetAttributeValue<double?>(ImportJob.Progress) ?? 0;

					if (currentProgress - progress > 5)
					{
						log.Log($"... imported {progress = currentProgress:0}% ...");
					}
				}
				catch
				{
					// ignored
				}
			}
			while (job?.GetAttributeValue<DateTime?>(ImportJob.CompletedOn) == null);
		}

		private bool ProcessErrorXml(string importXmlLog)
		{
			importXmlLog.RequireNotEmpty(nameof(importXmlLog));

			var isfailed = false;

			try
			{
				var doc = new XmlDocument();
				doc.LoadXml(importXmlLog);
				var error = doc.SelectSingleNode("//result[@result='failure']/@errortext")?.Value;

				if (error == null)
				{
					return false;
				}

				isfailed = true;
				log.LogError($"Import failed with the following error (Full log written to import.log):\r\n{error}.");
			}
			finally
			{
				if (isfailed)
				{
					try
					{
						var latestIndex = Directory.GetFiles(config.Path)
							.Select(f => Regex.Match(f, @"import(?:-(\d+))?\.log").Groups[1].Value)
							.Where(f => f.IsNotEmpty())
							.Select(int.Parse)
							.OrderByDescending(f => f)
							.FirstOrDefault();
						File.WriteAllText($"import{(latestIndex == 0 && !File.Exists("import.log") ? "" : $"-{latestIndex + 1}")}.log",
							importXmlLog);
					}
					catch
					{
						// ignored
					}
				}
			}

			return true;
		}

		private IEnumerable<SolutionInfo> LoadSolutions(string solutionName)
		{
			var paths = Directory.GetFiles(config.Path, "*.zip")
				.Where(f => Regex.IsMatch(f, solutionName))
				.OrderByDescending(f => f).ToArray();

			if (!paths.Any())
			{
				throw new ToolException(
					$"Couldn't find a solution matching pattern: '{solutionName}' in '{Path.GetDirectoryName(config.Path)}'.");
			}

			return
			paths.Select(
				path =>
				{
					log.Log($"Loading solution: {path} ...");

					var data = File.ReadAllBytes(path);
					var(name, version, isManaged) = GetSolutionInfo(data);

					return
						new SolutionInfo
						{
							Name = name,
							Version = version,
							IsManaged = isManaged,
							Data = data
						};
				});
		}

		private (string name, string version, bool isManaged) GetSolutionInfo(byte[] solution)
		{
			var doc = new XmlDocument();
			doc.LoadXml(ReadSolutionDefinition(solution));
			var name = doc.SelectSingleNode("//SolutionManifest/UniqueName")?.InnerText;
			var version = doc.SelectSingleNode("//Version")?.InnerText;
			var isManaged = doc.SelectSingleNode("//Managed")?.InnerText == "1";
			log.Log($"Local solution version: {version}, managed: {isManaged}.");

			return (name, version, isManaged);
		}

		private string ReadSolutionDefinition(byte[] solution)
		{
			var file = new ZipArchive(new MemoryStream(solution)).Entries
				.FirstOrDefault(x => x.Name.Equals("solution.xml", StringComparison.InvariantCulture));

			if (file == null)
			{
				throw new ToolException("Could not find solution definition file 'solution.xml' in solution file.");
			}

			return new StreamReader(file.Open(), Encoding.UTF8).ReadToEnd();
		}

		private void Publish()
		{
			log.Log("Publishing customisations ...");

			for (var i = 0; i < 3; i++)
			{
				Thread.Sleep(5000);

				try
				{
					service.Execute(new PublishAllXmlRequest());
					log.Log("[Finished] Publishing customisations.");
					break;
				}
				catch (Exception e)
				{
					log.Log(e);

					if (i < 2)
					{
						log.LogWarning("Retrying publish ...");
					}
				}
			}
		}

		private class SolutionInfo
		{
			internal string Name;
			internal string Version;
			internal bool IsManaged;
			internal byte[] Data;
		}
	}
}
