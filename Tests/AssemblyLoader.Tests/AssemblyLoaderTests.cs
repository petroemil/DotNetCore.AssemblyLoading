using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AssemblyLoader.Tests
{
    public class AssemblyLoaderTests
    {
        private static readonly FileInfo assembly1File;
        private static readonly FileInfo assembly2File;

        private static readonly (string, string) rx300AssemblyVersion = ("System.Reactive.Core, Version=3.0.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.0.0.0");
        private static readonly (string, string) rx310AssemblyVersion = ("System.Reactive.Core, Version=3.0.3000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.1.0.0");
        private static readonly (string, string) rx311AssemblyVersion = ("System.Reactive.Core, Version=3.0.3000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.1.1.0");

        private static readonly (string, string) ix310AssemblyVersion = ("System.Interactive, Version=3.0.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.1.0.0");
        private static readonly (string, string) ix311AssemblyVersion = ("System.Interactive, Version=3.0.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.1.1.0");

        static AssemblyLoaderTests()
        {
            // The AppContext.BaseDirectory will return the Test project's output directory
            // Something like <SolutionPath>\Tests\AssemblyLoader.Tests\bin\Debug\netcoreapp2.0
            var testsOutputDirectory = AppContext.BaseDirectory;

            // Step back 4 (1st -> Debug, 2nd -> bin, 3rd -> AssemblyLoader.Tests, 4th -> Tests
            var testBaseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));

            assembly1File = new FileInfo(ConstructAssemblyPath(testBaseDirectory, "Assembly-1"));
            assembly2File = new FileInfo(ConstructAssemblyPath(testBaseDirectory, "Assembly-2"));
        }

        private static string ConstructAssemblyPath(string baseDirectory, string assemblyName, string configuration = "Debug", string targetPlatform = "netstandard2.0")
            => $@"{baseDirectory}\TestAssemblies\{assemblyName}\bin\{configuration}\{targetPlatform}\{assemblyName}.dll";

        private static IEnumerable<(string, string)> CheckAssemblyDependencyVersion(Assembly assembly)
        {
            dynamic instance = assembly
                .GetTypes()
                .Single(type => type.Name == "Main")
                .GetConstructor(Type.EmptyTypes)
                .Invoke(null);

            yield return ((string, string))instance.CheckDependency1Version();
            yield return ((string, string))instance.CheckDependency2Version();
        }

        [Fact]
        public void ShareDependenciesBetweenAssembliesAndHost()
        {
            var assemblyLoader = new AssemblyLoader();
            var loadedAssemblies = assemblyLoader
                .UseFile(assembly1File)
                .UseFile(assembly2File)
                .Load();

            var hostAssembly = Assembly.GetExecutingAssembly();
            var assembly1 = loadedAssemblies[0];
            var assembly2 = loadedAssemblies[1];

            var hostAssemblyDependencyVersion = CheckAssemblyDependencyVersion(hostAssembly).ToArray();
            var assembly1DependencyVersion = CheckAssemblyDependencyVersion(assembly1).ToArray();
            var assembly2DependencyVersion = CheckAssemblyDependencyVersion(assembly2).ToArray();

            var hostAssemblyRxVersion = hostAssemblyDependencyVersion[0];

            var assembly1RxVersion = assembly1DependencyVersion[0];
            var assembly1IxVersion = assembly1DependencyVersion[1];

            var assembly2RxVersion = assembly2DependencyVersion[0];
            var assembly2IxVersion = assembly2DependencyVersion[1];

            Assert.Equal(rx300AssemblyVersion, hostAssemblyRxVersion);

            Assert.Equal(rx300AssemblyVersion, assembly1RxVersion);
            Assert.Equal(ix311AssemblyVersion, assembly1IxVersion);

            Assert.Equal(rx300AssemblyVersion, assembly2RxVersion);
            Assert.Equal(ix311AssemblyVersion, assembly2IxVersion);
        }

        [Fact]
        public void ShareDependenciesBetweenAssembliesButNotHost()
        {
            var assemblyLoader = new AssemblyLoader();
            var loadedAssemblies = assemblyLoader
                .DontShareDependenciesWithHost()
                .UseFile(assembly1File)
                .UseFile(assembly2File)
                .Load();

            var hostAssembly = Assembly.GetExecutingAssembly();
            var assembly1 = loadedAssemblies[0];
            var assembly2 = loadedAssemblies[1];

            var hostAssemblyDependencyVersion = CheckAssemblyDependencyVersion(hostAssembly).ToArray();
            var assembly1DependencyVersion = CheckAssemblyDependencyVersion(assembly1).ToArray();
            var assembly2DependencyVersion = CheckAssemblyDependencyVersion(assembly2).ToArray();

            var hostAssemblyRxVersion = hostAssemblyDependencyVersion[0];

            var assembly1RxVersion = assembly1DependencyVersion[0];
            var assembly1IxVersion = assembly1DependencyVersion[1];

            var assembly2RxVersion = assembly2DependencyVersion[0];
            var assembly2IxVersion = assembly2DependencyVersion[1];

            Assert.Equal(rx300AssemblyVersion, hostAssemblyRxVersion);

            Assert.Equal(rx311AssemblyVersion, assembly1RxVersion);
            Assert.Equal(ix311AssemblyVersion, assembly1IxVersion);

            Assert.Equal(rx311AssemblyVersion, assembly2RxVersion);
            Assert.Equal(ix311AssemblyVersion, assembly2IxVersion);
        }

        [Fact]
        public void DontShareDependenciesAtAll()
        {
            var assemblyLoader1 = new AssemblyLoader();
            var assembly1 = assemblyLoader1
                .DontShareDependenciesWithHost()
                .UseFile(assembly1File)
                .Load()
                .Single();

            var assemblyLoader2 = new AssemblyLoader();
            var assembly2 = assemblyLoader2
                .DontShareDependenciesWithHost()
                .UseFile(assembly2File)
                .Load()
                .Single();

            var hostAssembly = Assembly.GetExecutingAssembly();

            var hostAssemblyDependencyVersion = CheckAssemblyDependencyVersion(hostAssembly).ToArray();
            var assembly1DependencyVersion = CheckAssemblyDependencyVersion(assembly1).ToArray();
            var assembly2DependencyVersion = CheckAssemblyDependencyVersion(assembly2).ToArray();

            var hostAssemblyRxVersion = hostAssemblyDependencyVersion[0];

            var assembly1RxVersion = assembly1DependencyVersion[0];
            var assembly1IxVersion = assembly1DependencyVersion[1];

            var assembly2RxVersion = assembly2DependencyVersion[0];
            var assembly2IxVersion = assembly2DependencyVersion[1];

            Assert.Equal(rx300AssemblyVersion, hostAssemblyRxVersion);

            Assert.Equal(rx311AssemblyVersion, assembly1RxVersion);
            Assert.Equal(ix311AssemblyVersion, assembly1IxVersion);

            Assert.Equal(rx310AssemblyVersion, assembly2RxVersion);
            Assert.Equal(ix310AssemblyVersion, assembly2IxVersion);
        }

        [Fact]
        public void ShareDependenciesWithHostButNotBetweenAssemblies()
        {
            var assemblyLoader1 = new AssemblyLoader();
            var assembly1 = assemblyLoader1
                .UseFile(assembly1File)
                .Load()
                .Single();

            var assemblyLoader2 = new AssemblyLoader();
            var assembly2 = assemblyLoader2
                .UseFile(assembly2File)
                .Load()
                .Single();

            var hostAssembly = Assembly.GetExecutingAssembly();

            var hostAssemblyDependencyVersion = CheckAssemblyDependencyVersion(hostAssembly).ToArray();
            var assembly1DependencyVersion = CheckAssemblyDependencyVersion(assembly1).ToArray();
            var assembly2DependencyVersion = CheckAssemblyDependencyVersion(assembly2).ToArray();

            var hostAssemblyRxVersion = hostAssemblyDependencyVersion[0];

            var assembly1RxVersion = assembly1DependencyVersion[0];
            var assembly1IxVersion = assembly1DependencyVersion[1];

            var assembly2RxVersion = assembly2DependencyVersion[0];
            var assembly2IxVersion = assembly2DependencyVersion[1];

            Assert.Equal(rx300AssemblyVersion, hostAssemblyRxVersion);

            Assert.Equal(rx300AssemblyVersion, assembly1RxVersion);
            Assert.Equal(ix311AssemblyVersion, assembly1IxVersion);

            Assert.Equal(rx300AssemblyVersion, assembly2RxVersion);
            Assert.Equal(ix310AssemblyVersion, assembly2IxVersion);
        }
    }
}