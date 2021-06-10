#region Imports

using CommandLine;
using Yagasoft.Tools.CmdToolTemplate.Attributes;

#endregion

namespace Yagasoft.Tools.SolutionMaestro.Cmd.Args
{
	[Verb("sol")]
	public class Arguments : ToolArgumentsBase
	{
		[Option('z', "description", Required = false,
			HelpText = @"Orchestrate Dynamics solutions.
The order of operations if multiple are specified: export, import, unpack, pack, config file.
The tool executes the config file as well as the action switches if both are specified.")]
		public bool Dummy { get; set; }

		[Option('e', "export", Required = false,
			HelpText = @"Export solution with config template:
""{
  names:'<name-regex-scsv>',
  isDateFile:<true|false*>,
  isManaged:<true|false*>,
  isOverwrite:<true|false*>,
  isRetry:<true|false*>,
  isPublish:<true*|false>,
  path:<output-folder>
}"" (Asterisk = default value, and include quotes).
To ease copying: ""{names:'',isDateFile:,isManaged:,isOverwrite:,isRetry:,isPublish:,path:}"".
Exports multiple if semicolon-separated, in order. Regex can return multiple, which are all processed.
Overwrite works if 'date' param is 'false'.")]
		public string ExportConfig { get; set; }

		[Option('i', "import", Required = false,
			HelpText = @"Import solution with config template:
""{
  names:'<filename-regex-scsv>',
  isOverwrite:<true|false*>,
  isRetry:<true|false*>,
  isPublish:<true*|false>},
  path:<source-folder>
}"" (Asterisk = default value, and include quotes).
To ease copying: ""{names:'',isOverwrite:,isRetry:,isPublish:,path:}"".
Import multiple if semicolon-separated, in order. Regex can return multiple, which are all processed.")]
		public string ImportConfig { get; set; }

		[Option('u', "unpack", Required = false,
			HelpText = @"Unpack solution with config template:
""{
  names:'<sol-filename-regex-scsv>',
  isDateFile:<true|false*>,
  isOverwrite:<true|false*>,
  path:<output-folder>
}"" (Asterisk = default value, and include quotes).
To ease copying: ""{names:'',isDateFile:,isOverwrite:,path:}"".
Resulting folders take the same name as the solution file. The 'output-folder' is the parent folder.
Unpack multiple if semicolon-separated, in order. Regex can return multiple, which are all processed.
Overwrite works if 'date' param is 'false'.")]
		public string UnpackConfig { get; set; }

		[Option('p', "pack", Required = false,
			HelpText = @"Pack solution with config template:
""{
  names:'<folder-name-regex-scsv>',
  isDateFile:<true|false*>,
  isOverwrite:<true|false*>,
  path:<source-folder>
}"" (Asterisk = default value, and include quotes).
To ease copying: ""{names:'',isDateFile:,isOverwrite:,path:}"".
Resulting files take the same name as the solution folder. The 'source-folder' is the parent folder.
Unpack multiple if semicolon-separated, in order. Regex can return multiple, which are all processed.
Overwrite works if 'date' param is 'false'.")]
		public string PackConfig { get; set; }

		[Option('c', "config-file", Required = false, HelpText = "Configuration file to execute.")]
		public string ConfigFile { get; set; }

		[Option('s', "source-conn-file", Required = false, HelpText = "Source connection file.")]
		public string SourceConnectFile { get; set; }

		[Option('d', "dest-conn-file", Required = false, HelpText = "Destination connection file.")]
		public string DestConnectFile { get; set; }

		[Option('m', "source-connection-string", Required = false,
			HelpText = "Source connection string. Takes precedence over the file switch.")]
		public string SourceConnectString { get; set; }

		[Option('n', "dest-connection-string", Required = false,
			HelpText = "Destination connection string. Takes precedence over the file switch.")]
		public string DestConnectString { get; set; }
	}
}
