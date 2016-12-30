﻿using NiceIO;
using System;
using System.IO;
using System.Linq;

namespace RoslynBuilder
{
	class InstallCrossGenStep : IBuildStep
	{
		// Note: this version must match the version of CoreCLR that's currently used by Roslyn
		// If it doesn't match, it will complain about ClrJit version not matching when doing CrossGen
		const string kCoreClrVersion = "1.0.2";
		const string kCoreClrPackageName = "runtime.win7-x64.Microsoft.NETCore.Runtime.CoreCLR";

		private static NPath PackagesDirectory => KnownPaths.RoslynRoot.Combine("Artifacts", "Packages").EnsureDirectoryExists();

		public void Execute()
		{
			InstallPackage(kCoreClrPackageName, kCoreClrVersion);

			var coreClrPackageFolder = PackagesDirectory.Combine($"{kCoreClrPackageName}.{kCoreClrVersion}");
			var crossGen = coreClrPackageFolder.Combine("tools", "crossgen.exe");

			if (!crossGen.FileExists())
				throw new FileNotFoundException($"Failed to find {crossGen}. Did NuGet not restore it?");

			KnownPaths.CrossGen = crossGen;
		}

		private static void InstallPackage(string packageName, string version)
		{
			Console.WriteLine($"Installing {packageName}...");
			var nugetOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.NuGet, $"install {packageName} -Version {version} -outputdirectory {PackagesDirectory} -NonInteractive");
			Console.WriteLine(nugetOutput);
			Console.WriteLine($"Installing restored {packageName}.");
		}
	}
}
