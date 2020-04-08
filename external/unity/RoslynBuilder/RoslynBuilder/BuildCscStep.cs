using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;

namespace RoslynBuilder
{
	class BuildCscStep : IBuildStep
	{
		public void Execute()
		{
			BuildRoslynComponents("netcoreapp3.1", "win-x64", KnownPaths.CscWindowsBinariesDirectory);
			BuildRoslynComponents("netcoreapp3.1", "osx-x64", KnownPaths.CscMacBinariesDirectory);
			BuildRoslynComponents("netcoreapp3.1", "linux-x64", KnownPaths.CscLinuxBinariesDirectory);
		}

		private static void BuildRoslynComponents(string framework, string runtime, NPath outputDir)
		{
			BuildProject(framework, runtime, outputDir, KnownPaths.RoslynRoot.Combine("src", "Compilers", "CSharp", "csc"));
			BuildProject(framework, runtime, outputDir, KnownPaths.RoslynRoot.Combine("src", "Compilers", "Server", "VBCSCompiler"));
		}

		private static void BuildProject(string framework, string runtime, NPath outputDir, NPath projectDir)
		{
			var description = framework;
			if (!string.IsNullOrEmpty(runtime))
				description += "/" + runtime;

			Console.WriteLine($"Building {projectDir} for {description}...");

			var commandLineArgs = new List<string>()
			{
				"publish",
				"--configuration", "Release",
				// Allow restore as --self-contained builds are complicated without it https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-patch-selection
				//"--no-restore",
				projectDir.ToString(),
				$"-o", outputDir.ToString(),
				$"--framework", framework.ToString()
			};

			if (!string.IsNullOrEmpty(runtime))
			{
				commandLineArgs.Add("--self-contained");
				commandLineArgs.Add("--runtime");
				commandLineArgs.Add(runtime);
			 }

			commandLineArgs.Add("/p:UseShippingAssemblyVersion=true");
			commandLineArgs.Add("/p:OfficialBuild=true");
			commandLineArgs.Add("/p:UsingToolIbcOptimization=false");
			commandLineArgs.Add("/p:UsingToolVisualStudioIbcTraining=false");
			commandLineArgs.Add("/p:PublishReadyToRun=true");
			// turn off source link since it assumes Roslyn is checked out from a github repo
			commandLineArgs.Add("/p:EnableSourceLink=false");

			var args = commandLineArgs.Select(a => $"\"{a}\"").Aggregate((a, b) => a + " " + b);
			var dotnetOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.DotNet, args);

			Console.WriteLine(dotnetOutput);
			Console.WriteLine($"Successfully built {projectDir} for {description}.");
		}
	}
}
