using System.IO;

namespace AssemblyLoader
{
    public static class AssemblyLoaderExtensions
    {
        public static AssemblyLoader UseFile(this AssemblyLoader assemblyLoader, FileInfo assemblyFile)
        {
            assemblyLoader.Use(assemblyFile);
            return assemblyLoader;
        }

        public static AssemblyLoader DontShareDependenciesWithHost(this AssemblyLoader assemblyLoader)
        {
            assemblyLoader.ShareDependenciesWithHost = false;
            return assemblyLoader;
        }
    }
}
