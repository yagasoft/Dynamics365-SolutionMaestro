#region Imports

using System;
using System.IO;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Tools.CmdToolTemplate.Shell;
using Yagasoft.Tools.CmdToolTemplate.Tool;
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

		private ExportParams exportParams = new ExportParams();
		private ImportParams importParams = new ImportParams();

		public void Initialise(ShellArguments shellArguments, Arguments toolArguments, CrmLog crmLog)
		{
			shellArgs = shellArguments;
			args = toolArguments;
			log = crmLog;

			ParseConfiguration();
		}

		private void ParseConfiguration()
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
				ConnectionHelpers.GetConnectionString(args.SourceConnectFile);
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
				ConnectionHelpers.GetConnectionString(args.DestConnectFile);
				log.Log($"Dest connection string: {connectionStrings.Destination}.");
			}

			if (args.ExportConfig.IsFilled())
			{
				log.Log("Parsing export configuration ...");
				exportParams = ConfigHelpers.Deserialise<ExportParams>(args.ExportConfig);
				log.Log($"Export configuration: {args.ExportConfig}.");
			}

			if (args.ImportConfig.IsFilled())
			{
				log.Log("Parsing import configuration ...");
				importParams = ConfigHelpers.Deserialise<ImportParams>(args.ImportConfig);
				log.Log($"Import configuration: {args.ImportConfig}.");
			}

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

		public void Run()
		{
			if (exportParams?.Names.IsFilled() == true)
			{
				sourceService.Require(nameof(sourceService), "Could not establish a connection to source server."
					+ " Review that you passed a proper connection string or file.");
				var tool = new ExportSolution(sourceService, log);
				tool.ExportSolutions(exportParams);
			}
		}

		private IOrganizationService ConnectToCrm(string connectionString)
		{
			return EnhancedServiceHelper.GetPoolingService(connectionString, 50);
		}
	}
}
