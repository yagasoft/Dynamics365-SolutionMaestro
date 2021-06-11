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
  names:'<name-regex-scsv>',	// semicolon-separated regex for names; if multiple are returned, done in order.
  isDateFile:<true|false*>,		// add the date and time as of the time of writing the file.
  isManaged:<true|false*>,		// export as Managed.
  isOverwrite:<true|false*>,	// overwrite the file if it already exists; takes effect only when 'isDateFile' is 'false'.
  isRetry:<true|false*>,		// try one more time if an error occurs.
  isPublish:<true*|false>,		// publish before export, done only once at the start.
  path:'<output-folder>'		// the base folder to store/read everything.
}"" (Asterisk = default value, and include quotes).
For ease of copying: ""{names:'',isDateFile:,isManaged:,isOverwrite:,isRetry:,isPublish:,path:''}"".")]
		public string ExportConfig { get; set; }

		[Option('i', "import", Required = false,
			HelpText = @"Import solution with config template:
""{
  names:'<filename-regex-scsv>',
  isOverwrite:<true|false*>,	// overwrite the solution if it already exists.
  isClean:<true|false*>,		// overwrite unmanaged customisations.
  isRetry:<true|false*>,
  isPublish:<true*|false>},		// publish after import, done only once at the end.
  path:'<source-folder>'
}"" (Asterisk = default value, and include quotes).
For ease of copying: ""{names:'',isOverwrite:,isClean:,isRetry:,isPublish:,path:''}"".")]
		public string ImportConfig { get; set; }

		[Option('u', "unpack", Required = false,
			HelpText = @"Unpack solution with config template:
""{
  names:'<sol-filename-regex-scsv>',
  isDateFile:<true|false*>,
  isOverwrite:<true|false*>,
  path:'<output-folder>'
}"" (Asterisk = default value, and include quotes).
For ease of copying: ""{names:'',isDateFile:,isOverwrite:,path:''}"".
Resulting folders take the same name as the solution file. The 'output-folder' is the parent folder.")]
		public string UnpackConfig { get; set; }

		[Option('p', "pack", Required = false,
			HelpText = @"Pack solution with config template:
""{
  names:'<folder-name-regex-scsv>',
  isDateFile:<true|false*>,
  isOverwrite:<true|false*>,
  path:'<source-folder>'
}"" (Asterisk = default value, and include quotes).
For ease of copying: ""{names:'',isDateFile:,isOverwrite:,path:''}"".
Resulting files take the same name as the solution folder. The 'source-folder' is the parent folder.")]
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
