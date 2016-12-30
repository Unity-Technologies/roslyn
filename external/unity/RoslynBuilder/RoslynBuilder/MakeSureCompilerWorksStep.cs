﻿using System;
using NiceIO;

namespace RoslynBuilder
{
	class MakeSureCompilerWorksStep : IBuildStep
	{
		const string kExpectedOutput = "Hello, world! This is a program compiled by Roslyn C# compiler.";
		
		public void Execute()
		{
			Console.WriteLine("Verifying that the C# compiler works...");

			var programPath = KnownPaths.RoslynRoot.Combine("Artifacts", "Test Program", "Program.cs");
			var executablePath = programPath.ChangeExtension(".exe");

			WriteTestProgram(programPath);

			var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToNPath();
			var mscorlibPath = windowsDirectory.Combine("Microsoft.NET", "Framework", "v4.0.30319", "mscorlib.dll");

			var coreRun = KnownPaths.CscBinariesDirectory.Combine("CoreRun.exe");
			var args = $"{KnownPaths.CscBinariesDirectory.Combine("csc.exe").InQuotes()} /REFERENCE:{mscorlibPath.InQuotes()} {programPath.InQuotes()} /OUT:{executablePath.InQuotes()}";
			var compilerOutput = Shell.ExecuteAndCaptureOutput(coreRun, args);
			Console.WriteLine(compilerOutput);

			var programOutput = Shell.ExecuteAndCaptureOutput(executablePath, string.Empty).Trim();
			if (programOutput != kExpectedOutput)
			{
				throw new InvalidProgramException(
					$"The compiled program did not output expected message." + Environment.NewLine +
					$"Expected: {kExpectedOutput}" + Environment.NewLine +
					$"Actual: {programOutput}");
			}

			Console.WriteLine("Successfully verified that the C# compiler works.");
		}

		private void WriteTestProgram(NPath programPath)
		{
			programPath.WriteAllLines(new string[]
			{
				@"using System;",
				"",
				"class Program",
				"{",
				"	static void Main()",
				"	{",
				"		Console.WriteLine(\"" + kExpectedOutput + "\");",
				"	}",
				"}",
			});
		}
	}
}