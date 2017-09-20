using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AssemblyLoader;

namespace ServiceHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Output is in <ProjectFolder>/bin/<Configuration>/<TargetFramework>
            var  baseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));

            var assemblyLoader = new AssemblyLoader.AssemblyLoader();
            var loadedAssemblies = assemblyLoader
                .UseFile(new FileInfo($@"{baseDirectory}\Version1\bin\Debug\netstandard2.0\Version1.dll"))
                .UseFile(new FileInfo($@"{baseDirectory}\Version2\bin\Debug\netstandard2.0\Version2.dll"))
                .Load();

            var assemblyLoader2 = new AssemblyLoader.AssemblyLoader();
            var loadedAssembly = assemblyLoader2
                .UseFile(new FileInfo($@"{baseDirectory}\Version3\bin\Debug\netstandard2.0\Version3.dll"))
                .Load();

            RunServices(loadedAssemblies);
            RunServices(loadedAssembly);

            Console.ReadLine();
        }

        static void RunServices(Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                dynamic instance = assembly
                    .GetTypes()
                    .Single(type => type.Name == "Main")
                    .GetConstructor(Type.EmptyTypes)
                    .Invoke(null);

                instance.Run();
            }
        }
    }
}
