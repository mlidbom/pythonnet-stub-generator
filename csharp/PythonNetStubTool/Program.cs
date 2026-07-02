using System.CommandLine;
using System.CommandLine.Parsing;
using PythonNetStubGenerator;

namespace PythonNetStubTool
{
    static class Program
    {
        static int Main(string[] args)
        {
            Option<DirectoryInfo> destPathOption = new("--dest-path")
            {
                Description = "Path to save the stubs to.",
                Required = true
            };

            Option<string> targetDllsOption = new("--target-dlls")
            {
                Description = "Target DLLs, separated by commas.",
                Required = true
            };

            Option<DirectoryInfo[]> searchPathsOption = new("--search-paths")
            {
                Description = "Path to search for referenced assemblies.",
                Arity = ArgumentArity.OneOrMore,
                Required = false
            };

            Option<bool> onlyTargetTypesOption = new("--only-target-types")
            {
                Description = "Only generate stubs for target assemblies (faster, excludes System.* types).",
                Required = false
            };

            Option<bool> addStubSuffixOption = new("--add-stubs-suffix")
            {
                Description = "Add a '-stubs' suffix to generated stub folders (for Pycharm).",
                Required = false
            };

            RootCommand rootCommand = new("PythonNet Stub Generator Tool");
            rootCommand.Options.Add(destPathOption);
            rootCommand.Options.Add(targetDllsOption);
            rootCommand.Options.Add(searchPathsOption);
            rootCommand.Options.Add(onlyTargetTypesOption);
            rootCommand.Options.Add(addStubSuffixOption);

            rootCommand.SetAction(parseResult =>
            {
                DirectoryInfo destPath = parseResult.GetValue(destPathOption)!;
                string targetDlls = parseResult.GetValue(targetDllsOption)!;
                DirectoryInfo[]? searchPaths = parseResult.GetValue(searchPathsOption);
                bool onlyTargetTypes = parseResult.GetValue(onlyTargetTypesOption);
                bool addStubSuffix = parseResult.GetValue(addStubSuffixOption);
                return Run(destPath, targetDlls, searchPaths, onlyTargetTypes, addStubSuffix);
            });

            ParseResult parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }

        /// <summary>
        /// Creates stubs for Python.Net
        /// </summary>
        /// <param name="destPath">Path to save the subs to.</param>
        /// <param name="searchPaths">Path to search for referenced assemblies</param>
        /// <param name="targetDlls">Target DLLs, separated by commas.</param>
        static int Run(
            DirectoryInfo destPath,
            string targetDlls,
            DirectoryInfo[]? searchPaths = null,
            bool onlyTargetTypes = false,
            bool addStubSuffix = false
            )
        {
            if (searchPaths != null)
            {
                foreach (var searchPath in searchPaths)
                    Console.WriteLine($"search path {searchPath}");
            }

            var infos = new List<FileInfo>();
            foreach (var pathStr in targetDlls.Split(','))
            {
                var assemblyPath = new FileInfo(pathStr.Trim());
                if (!assemblyPath.Exists)
                {
                    Console.WriteLine($"error: can not find {assemblyPath}");
                    return -1;
                }
                infos.Add(assemblyPath);
            }

            Console.WriteLine($"building stubs...");

            try
            {
                var dest = StaticStubBuilder.BuildAssemblyStubs(destPath, infos.ToArray(), searchPaths, onlyTargetTypes, addStubSuffix);
                Console.WriteLine($"stubs saved to {dest}");
                return 0;
            }
            catch (Exception sgEx)
            {
                Console.WriteLine($"error: failed generating stubs | {sgEx.Message}");
                throw;
            }
        }
    }
}
