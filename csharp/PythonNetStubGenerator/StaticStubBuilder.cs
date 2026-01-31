using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PythonNetStubGenerator
{
    public static class StaticStubBuilder
    {
        private static HashSet<DirectoryInfo> SearchPaths { get; } = new HashSet<DirectoryInfo>();
        private static HashSet<string> TargetAssemblyNames { get; } = new HashSet<string>();

        public static DirectoryInfo BuildAssemblyStubs(DirectoryInfo destPath, FileInfo[] targetAssemblyPaths, DirectoryInfo[] searchPaths = null, bool onlyTargetTypes = false)
        {
            // prepare resolver
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            // pick a dll and load
            foreach (var targetAssemblyPath in targetAssemblyPaths)
            {
                var assemblyToStub = Assembly.LoadFrom(targetAssemblyPath.FullName);
                SearchPaths.Add(targetAssemblyPath.Directory);
                
                // Track this as a target assembly
                TargetAssemblyNames.Add(assemblyToStub.GetName().Name);

                if (searchPaths != null)
                    foreach (var path in SearchPaths)
                        SearchPaths.Add(path);

                Console.WriteLine($"Generating Assembly: {assemblyToStub.FullName}");
                foreach (var exportedType in assemblyToStub.GetExportedTypes())
                {
                    if (!exportedType.IsVisible)
                        continue;
                    StaticPythonTypes.AddDependency(exportedType);
                }
            }


            var typeAssembly = typeof(Type).Assembly;
            Console.WriteLine($"Generating Built-in Assembly: {typeAssembly.FullName}");

            foreach (var exportedType in typeAssembly.GetExportedTypes())
            {
                if (!exportedType.IsVisible)
                    continue;
                StaticPythonTypes.AddDependency(exportedType);
            }

            var consoleAssembly = typeof(Console).Assembly;
            Console.WriteLine($"Generating Built-in Assembly: {consoleAssembly.FullName}");
            foreach (var exportedType in consoleAssembly.GetExportedTypes())
            {
                if (!exportedType.IsVisible)
                    continue;
                StaticPythonTypes.AddDependency(exportedType);
            }


            while (true)
            {
                var (nameSpace, types) = StaticPythonTypes.RemoveDirtyNamespace();

                if (nameSpace == "")
                    break;
                
                List<Type> typesToGenerate;
                if (onlyTargetTypes)
                {
                    typesToGenerate = types.Where(t => TargetAssemblyNames.Contains(t.Assembly.GetName().Name)).ToList();
                    if (typesToGenerate.Count == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    typesToGenerate = types.ToList();
                }
                
                WriteStub(destPath, nameSpace, typesToGenerate);
            }


            return destPath;
        }

        internal static void WriteStub(DirectoryInfo rootDirectory, string nameSpace, IEnumerable<Type> stubTypes)
        {
            // sort the stub list so we get consistent output over time
            var orderedTypes = stubTypes.OrderBy(it => it.Name);

            string path;

            if (nameSpace is null)
            {
                path = "global_";
            }
            else
            {
                var split = nameSpace.Split('.');

                if (split[0] == "global_")
                {
                    throw new InvalidDataException("The namespace \"global_\" is reserved.");
                }

                path = split.Aggregate(rootDirectory.FullName, Path.Combine);
            }

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, "__init__.pyi");

            StaticPythonTypes.ClearCurrent();

            var stubText = StaticStubWriter.GetStub(nameSpace, orderedTypes);


            File.WriteAllText(path, stubText);
        }


        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var parts = args.Name.Split(',');

            var assemblyToResolve = $"{parts[0]}.dll";

            // try to find the dll in given search paths
            foreach (var searchPath in SearchPaths)
            {
                var assemblyPath = Path.Combine(searchPath.FullName, assemblyToResolve);
                if (File.Exists(assemblyPath))
                    return Assembly.LoadFrom(assemblyPath);
            }

            return null;

        }
    }
}
