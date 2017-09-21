using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AssemblyLoader.Tests
{
    public class AssemblyLoaderTests
    {
        private readonly string assembly1Path;
        private readonly string assembly2Path;

        private readonly (string, string) rx300AssemblyVersion = ("System.Reactive.Core, Version=3.0.0.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.0.0.0");
        private readonly (string, string) rx310AssemblyVersion = ("System.Reactive.Core, Version=3.0.3000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.1.0.0");
        private readonly (string, string) rx311AssemblyVersion = ("System.Reactive.Core, Version=3.0.3000.0, Culture=neutral, PublicKeyToken=94bc3704cddfc263", "3.1.1.0");

        public AssemblyLoaderTests()
        {
            // The AppContext.BaseDirectory will return the Test project's output directory
            // Something like <SolutionPath>\Tests\AssemblyLoader.Tests\bin\Debug\netcoreapp2.0
            var testsOutputDirectory = AppContext.BaseDirectory;

            // Step back 4 (1st -> Debug, 2nd -> bin, 3rd -> AssemblyLoader.Tests, 4th -> Tests
            var testBaseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));

            this.assembly1Path = ConstructAssemblyPath(testBaseDirectory, "Assembly-1");
            this.assembly2Path = ConstructAssemblyPath(testBaseDirectory, "Assembly-2");
        }

        private string ConstructAssemblyPath(string baseDirectory, string assemblyName, string configuration = "Debug", string targetPlatform = "netstandard2.0")
            => $@"{baseDirectory}\TestAssemblies\{assemblyName}\bin\{configuration}\{targetPlatform}\{assemblyName}.dll";

        private (string, string) CheckAssemblyDependencyVersion(Assembly assembly)
        {
            dynamic instance = assembly
                .GetTypes()
                .Single(type => type.Name == "Main")
                .GetConstructor(Type.EmptyTypes)
                .Invoke(null);

            return ((string, string))instance.CheckAssemblyVersion();
        }

        [Fact]
        public void ShareDependenciesBetweenAssembliesAndHost()
        {
            var assemblyLoader = new AssemblyLoader();
            var loadedAssemblies = assemblyLoader
                .UseFile(new FileInfo(this.assembly1Path))
                .UseFile(new FileInfo(this.assembly2Path))
                .Load();

            var hostAssembly = Assembly.GetExecutingAssembly();
            var assembly1 = loadedAssemblies[0];
            var assembly2 = loadedAssemblies[1];

            var hostAssemblyDependencyVersion = CheckAssemblyDependencyVersion(hostAssembly);
            var assembly1DependencyVersion = CheckAssemblyDependencyVersion(assembly1);
            var assembly2DependencyVersion = CheckAssemblyDependencyVersion(assembly2);

            Assert.Equal(rx300AssemblyVersion, hostAssemblyDependencyVersion);
            Assert.Equal(rx300AssemblyVersion, assembly1DependencyVersion);
            Assert.Equal(rx300AssemblyVersion, assembly2DependencyVersion);
        }

        [Fact]
        public void ShareDependenciesBetweenAssembliesButNotHost()
        {
            var assemblyLoader = new AssemblyLoader();
            var loadedAssemblies = assemblyLoader
                .DontShareDependenciesWithHost()
                .UseFile(new FileInfo(this.assembly1Path))
                .UseFile(new FileInfo(this.assembly2Path))
                .Load();

            var hostAssembly = Assembly.GetExecutingAssembly();
            var assembly1 = loadedAssemblies[0];
            var assembly2 = loadedAssemblies[1];

            var hostAssemblyDependencyVersion = CheckAssemblyDependencyVersion(hostAssembly);
            var assembly1DependencyVersion = CheckAssemblyDependencyVersion(assembly1);
            var assembly2DependencyVersion = CheckAssemblyDependencyVersion(assembly2);

            Assert.Equal(rx300AssemblyVersion, hostAssemblyDependencyVersion);
            Assert.Equal(rx311AssemblyVersion, assembly1DependencyVersion);
            Assert.Equal(rx311AssemblyVersion, assembly2DependencyVersion);
        }

        [Fact]
        public void DontShareDependenciesAtAll()
        {
            var assemblyLoader1 = new AssemblyLoader();
            var assembly1 = assemblyLoader1
                .DontShareDependenciesWithHost()
                .UseFile(new FileInfo(this.assembly1Path))
                .Load()
                .Single();

            var assemblyLoader2 = new AssemblyLoader();
            var assembly2 = assemblyLoader2
                .DontShareDependenciesWithHost()
                .UseFile(new FileInfo(this.assembly2Path))
                .Load()
                .Single();

            var hostAssembly = Assembly.GetExecutingAssembly();

            var hostAssemblyDependencyVersion = CheckAssemblyDependencyVersion(hostAssembly);
            var assembly1DependencyVersion = CheckAssemblyDependencyVersion(assembly1);
            var assembly2DependencyVersion = CheckAssemblyDependencyVersion(assembly2);

            Assert.Equal(rx300AssemblyVersion, hostAssemblyDependencyVersion);
            Assert.Equal(rx311AssemblyVersion, assembly1DependencyVersion);
            Assert.Equal(rx310AssemblyVersion, assembly2DependencyVersion);
        }
    }
}