using System;
using System.IO;

namespace RoslynBuilder
{
    class ReplaceCoreCLRStep : IBuildStep
    {
        // There is a bug in .NET Core 3.1 that sometimes causes execution to jump to the middle of an x64 instruction on macOS: https://github.com/dotnet/runtime/pull/38242
        // This specific issue seems to always reproduce when running on Apple silicon devices under Rosetta 2 emulation
        // To work around this issue, we use a custom version of CoreCLR with a fix applied.
        // We build this custom CoreCLR version on Yamato: https://yamato.prd.cds.internal.unity3d.com/jobs/818-CoreCLR-Mono/tree/apple-silicon-fix
        public void Execute()
        {
            // Extract custom CoreCLR
            var destDir = KnownPaths.ArtifactsDirectory.Combine("CoreCLR");

            if (destDir.DirectoryExists())
                destDir.DeleteContents(NiceIO.DeleteMode.Soft);

            var unzipOutput = Shell.ExecuteAndCaptureOutput(KnownPaths.SevenZip, $"x {KnownPaths.CustomCoreClrBuildsZip.InQuotes()} -o{destDir.InQuotes()} -r -y");
            Console.WriteLine(unzipOutput);

            var coreClrDir = destDir.Combine("OSX.x64.Release");
            Console.WriteLine($"Replacing CoreCLR runtime for macOS from {coreClrDir}.");

            // Overwrite macOS binaries
            var filesToOverwrite = new[]
            {
                "libclrjit.dylib",
                "libcoreclr.dylib",
                "libdbgshim.dylib",
                "libmscordaccore.dylib",
                "libmscordbi.dylib",
                "System.Globalization.Native.dylib",
                "System.Private.CoreLib.dll",
            };

            foreach (var file in filesToOverwrite)
            {
                var sourceFile = coreClrDir.Combine(file);
                if (!sourceFile.Exists())
                    throw new FileNotFoundException($"Could not find expected CoreCLR file ('{sourceFile}').");

                sourceFile.Copy(KnownPaths.CscMacBinariesDirectory);
            }
        }
    }
}
