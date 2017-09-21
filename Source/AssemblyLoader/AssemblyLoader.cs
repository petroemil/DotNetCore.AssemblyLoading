using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace AssemblyLoader
{
    public class AssemblyLoader
    {
        private List<FileInfo> assembliesToLoad = new List<FileInfo>();
        private List<(string dllName, Version version, FileInfo fileInfo)> dependencyPool = new List<(string dllName, Version version, FileInfo fileInfo)>();

        public bool ShareDependenciesWithHost { get; set; } = true;

        public static void MakeSnapshotOfLoadedAssemblies()
        {
            DirectoryLoader.assembliesLoadedAtStartup = AppDomain.CurrentDomain.GetAssemblies();
        }

        public void Use(FileInfo assemblyFile)
        {
            this.assembliesToLoad.Add(assemblyFile);

            var dependencies = ScanDependencies(assemblyFile);
            this.dependencyPool.AddRange(dependencies);
        }

        private static List<(string dllName, Version version, FileInfo fileInfo)> ScanDependencies(FileInfo assemblyFile)
        {
            var dependencies = assemblyFile.Directory
                .GetFiles("*.dll")
                .Where(fileInfo => fileInfo != assemblyFile)
                .Select(dependency => InspectDll(dependency))
                .ToList();

            return dependencies;
        }

        private static (string dllName, Version version, FileInfo fileInfo) InspectDll(FileInfo assemblyFile)
        {
            Version version;
            try
            {
                // Try to retrieve File Version, as it's usually more up-to-date than Assembly Version
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assemblyFile.FullName).FileVersion;
                var fileVersion = Version.Parse(fileVersionInfo);

                version = fileVersion;
            }
            catch
            {
                // Fall back to provide Assembly Version
                var assemblyName = AssemblyName.GetAssemblyName(assemblyFile.FullName);
                var assemblyVersion = assemblyName.Version;

                version = assemblyVersion;
            }

            var assemblyFileNameWithoutExtension = assemblyFile.Name.Replace(assemblyFile.Extension, "");
            return (assemblyFileNameWithoutExtension, version, assemblyFile);
        }

        public Assembly[] Load()
        {
            // Deduplicate the pool by eliminating duplicates and only keeping the latest versions
            var mergedPool = dependencyPool
                .GroupBy(assemblyInfo => assemblyInfo.dllName)
                .Select(group => group
                    .OrderByDescending(assemblyInfo => assemblyInfo.version)
                    .First())
                .ToList();

            var assemblyLoader = new DirectoryLoader(mergedPool, this.ShareDependenciesWithHost);

            var loadedAssemblies = this.assembliesToLoad
                .Select(assemblyFile => assemblyLoader.LoadFromAssemblyPath(assemblyFile.FullName))
                .ToArray();

            return loadedAssemblies;
        }

        private class DirectoryLoader : AssemblyLoadContext
        {
            public static Assembly[] assembliesLoadedAtStartup;

            private readonly List<(string dllName, Version version, FileInfo fileInfo)> dependencyPool;
            private readonly List<Assembly> loadedDependencies;
            private readonly bool shareDependenciesWithHost;

            public DirectoryLoader(List<(string dllName, Version version, FileInfo fileInfo)> dependencyPool, bool shareDependenciesWithHost)
            {
                this.dependencyPool = dependencyPool;
                this.loadedDependencies = new List<Assembly>();
                this.shareDependenciesWithHost = shareDependenciesWithHost;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (this.shareDependenciesWithHost)
                {
                    // Try to use an assembly that is already loaded (no strong version match is forced)
                    var existingSameOrNewerVersionAssembly = assembliesLoadedAtStartup.SingleOrDefault(a => a.GetName().Name == assemblyName.Name);
                    if (existingSameOrNewerVersionAssembly != null) return existingSameOrNewerVersionAssembly;
                }

                try
                {
                    // Try to use the default assembly loader
                    return Default.LoadFromAssemblyName(assemblyName);
                }
                catch
                {
                    // Try to use an already loaded assembly from the current context
                    var existingAssembly = this.loadedDependencies.SingleOrDefault(a => a.FullName == assemblyName.FullName);
                    if (existingAssembly != null)
                        return existingAssembly;

                    // Fall back to load assembly from known directory using the "DependencyPool"
                    var loadedAssembly = LoadFromAssemblyPath(this.dependencyPool.Single(x => x.dllName == assemblyName.Name).fileInfo.FullName);
                    this.loadedDependencies.Add(loadedAssembly);
                    return loadedAssembly;
                }
            }
        }
    }
}
