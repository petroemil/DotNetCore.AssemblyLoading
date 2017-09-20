using System;
using System.Reflection;

namespace Version3
{
    public class Main
    {
        public void Run()
        {
            CheckAssemblyVersion();
        }

        static void CheckAssemblyVersion()
        {
            var assembly = typeof(System.Reactive.Observer).Assembly;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            var assemblyFullName = assembly.FullName;
            Console.WriteLine($"v3: {assemblyFullName} (File Version: {fileVersion})");
        }
    }
}
