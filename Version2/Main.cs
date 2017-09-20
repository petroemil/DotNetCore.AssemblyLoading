using System;
using System.Reflection;

namespace Version2
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
            Console.WriteLine($"v2: {assemblyFullName} (File Version: {fileVersion})");
        }
    }
}
