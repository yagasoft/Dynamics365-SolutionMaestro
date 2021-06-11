#region Imports

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Tools.CmdToolTemplate.Shell;
using Yagasoft.Tools.CmdToolTemplate.Tool;
using Yagasoft.Tools.Common.Exceptions;
using Yagasoft.Tools.Common.Helpers;
using Yagasoft.Tools.SolutionMaestro.Cmd.Args;
using Yagasoft.Tools.SolutionMaestro.Core.IO;
using Yagasoft.Tools.SolutionMaestro.Core.Parameters;
using ConnectionHelpers = Yagasoft.Tools.Common.Helpers.ConnectionHelpers;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Cmd
{
	public class SolutionMaestroCmdTool : ICmdTool<Arguments>
	{
		private ShellArguments shellArgs;
		private Arguments args;
		private CrmLog log;

		private readonly ConnectionStrings connectionStrings = new ConnectionStrings();
		private IOrganizationService sourceService;
		private IOrganizationService destService;

		private readonly List<ExportParams> exportParams = new List<ExportParams>();
		private readonly List<ImportParams> importParams = new List<ImportParams>();

		public void Initialise(ShellArguments shellArguments, Arguments toolArguments, CrmLog crmLog)
		{
			shellArgs = shellArguments;
			args = toolArguments;
			log = crmLog;

			ParseConnections();
			ParseConfigs();
			Connect();
		}

		public void Run()
		{
			if (exportParams.Any())
			{
				var exporter = new ExportSolution(sourceService, log);

				foreach (var exportParam in exportParams)
				{
					if (exportParam?.Names.IsFilled() == true)
					{
						if (sourceService == null)
						{
							throw new ToolException("Could not establish a connection to source server."
								+ " Make sure that you passed a proper connection string or file.");
						}

						exporter.Export(exportParam);
					}
				}
			}

			if (importParams.Any())
			{
				var importer = new ImportSolution(sourceService, log);

				foreach (var importParam in importParams)
				{
					if (importParam?.Names.IsFilled() == true)
					{
						if (destService == null)
						{
							throw new ToolException("Could not establish a connection to destination server."
								+ " Make sure that you passed a proper connection string or file.");
						}

						importer.ImportSolutions(importParam);
						importer.Finalise();
					}
				}
			}
		}

		private void ParseConnections()
		{
			if (args.SourceConnectString.IsFilled())
			{
				log.Log("Parsing source connection string ...");
				connectionStrings.Source = args.SourceConnectString;
				log.Log($"Source connection string: {connectionStrings.Source}.");
			}
			else if (args.SourceConnectFile.IsFilled() && File.Exists(args.SourceConnectFile))
			{
				log.Log("Parsing source connection file ...");
				connectionStrings.Source = ConnectionHelpers.GetConnectionString(args.SourceConnectFile).ConnectionString;
				log.Log($"Source connection string: {connectionStrings.Source}.");
			}

			if (args.DestConnectString.IsFilled())
			{
				log.Log("Parsing dest connection string ...");
				connectionStrings.Destination = args.DestConnectString;
				log.Log($"Dest connection string: {connectionStrings.Destination}.");
			}
			else if (args.DestConnectFile.IsFilled() && File.Exists(args.DestConnectFile))
			{
				log.Log("Parsing dest connection file ...");
				connectionStrings.Destination = ConnectionHelpers.GetConnectionString(args.DestConnectFile).ConnectionString;
				log.Log($"Dest connection string: {connectionStrings.Destination}.");
			}
		}

		private void ParseConfigs()
		{
			if (args.ExportConfig.IsFilled())
			{
				log.Log("Parsing export configuration ...");
				exportParams.Add(ConfigHelpers.Deserialise<ExportParams>(args.ExportConfig.Insert(1, "operation:'export',")));
				log.Log($"Export configuration: {args.ExportConfig}.");
			}

			if (args.ImportConfig.IsFilled())
			{
				log.Log("Parsing import configuration ...");
				importParams.Add(ConfigHelpers.Deserialise<ImportParams>(args.ImportConfig.Insert(1, "operation:'import',")));
				log.Log($"Import configuration: {args.ImportConfig}.");
			}

			if (args.ConfigFile.IsFilled() && File.Exists(args.ConfigFile))
			{
				log.Log("Parsing config file ...");
				var config = ConfigHelpers.GetConfigurationParams<SolutionParams>(args.ConfigFile);
				log.Log($"Parsed config file.");

				if (config.Pipeline != null)
				{
					exportParams.AddRange(config.Pipeline.Where(p => p is ExportParams).Cast<ExportParams>());
					importParams.AddRange(config.Pipeline.Where(p => p is ImportParams).Cast<ImportParams>());
				}

				if (config.Global != null)
				{
					foreach (var exportParam in exportParams)
					{
						OverwriteConfig(config.Global, exportParam);
					}

					foreach (var importParam in importParams)
					{
						OverwriteConfig(config.Global, importParam);
					}
				}
			}
		}

		private void OverwriteConfig(ParamsBase sourceConfig, ParamsBase targetConfig)
		{
			foreach (var gp in sourceConfig.GetType().GetProperties())
			{
				foreach (var ep in targetConfig.GetType().GetProperties()
					.Where(p => p.Name == gp.Name))
				{
					var value = gp.GetValue(sourceConfig);

					if (value != null)
					{
						ep.SetValue(targetConfig, value);
					}
				}
			}
		}

		private void Connect()
		{
			if (connectionStrings.Source.IsFilled())
			{
				log.Log("Connecting to source ...");
				sourceService = ConnectToCrm(connectionStrings.Source);
				log.Log("Connected to source.");
			}

			if (connectionStrings.Destination.IsFilled())
			{
				log.Log("Connecting to dest ...");
				destService = ConnectToCrm(connectionStrings.Destination);
				log.Log("Connected to dest.");
			}
		}

		private IOrganizationService ConnectToCrm(string connectionString)
		{
			return EnhancedServiceHelper.GetPoolingService(connectionString, 50);
		}
	}
}
