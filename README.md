
Assembly loading in .NET Core is quite different compared to the .NET Framework. After a bit of research I realised that a lot of people have questions and confusions around this topic, so I made a sample solution to demonstrate how assemblies can be loaded properly in runtime.

The very first thing that worth mentioning, is that .NET Core or Standard class libraries, by default, won't copy their dependencies to their output directory after build. To force this, an attribute have to be added to the class library's csproj file:

```XML
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
```

The second thing to notice, is that even with the dependencies in place, if you try to load an assembly with the traditional `Assembly.LoadFrom()` approach, it will throw exceptions, saying it can't find the dependencies. Long story short, the default behaviour won't scan the surroundings of the assembly that you are trying to load.
To deal with this, you have to implement a custom `AssemblyLoaderContext` class that can fall back to look for libraries in certain folders.

```CSharp
private class DirectoryLoader : AssemblyLoadContext
{
    // ...

    protected override Assembly Load(AssemblyName assemblyName)
    {
        // ...

        try
        {
            // Try to use the default assembly loader
            return Default.LoadFromAssemblyName(assemblyName);
        }
        catch
        {
            // ...

            // Fall back to load assembly from known directory using the "DependencyPool"
            var loadedAssembly = LoadFromAssemblyPath(this.dependencyPool.Single(x => x.dllName == assemblyName.Name).fileInfo.FullName);
            this.loadedDependencies.Add(loadedAssembly);
            return loadedAssembly;
        }
    }
}
```

The last thing is that .NET Core can load multiple versions of the same assembly, and can build up the dependency tree independently for multiple dynamically loaded assemblies within the same process. It means, if you have 2 assemblies with a reference to the same NuGet package but with different versions, with the proper redirects in the `AssemblyLoaderContext` you can actually load both assemblies with their corresponding dependencies and they will work just fine. It will even load the very same version of the (dependency) assembly twice if you instruct it to do. 
The problem with multiple assembly references is that even if they have same version, passing around objects from one dynamically loaded assembly to another will cause an exception. That kind of communication will only work if those two assemblies share an AssemblyReference to their dependencies. But if they don't actually reference the same version of the NuGet package, then we need some logic to decide which assembly version to go for. Also what if a version of the assembly is already loaded because the host application has a "hard reference" on it? It will reuse the already loaded version, not caring about the version....

As you can see, it can be quite difficult to get assembly loading right.

To help people with the same (or similar) issue, I put together a sample application that shows some of the techniques to deal with assembly loading in runtime.

The implementation of the AssemblyLoader can be found in the [Source/AssemblyLoader/AssemblyLoader.cs](Source/AssemblyLoader/AssemblyLoader.cs) file, and you can see some examples for usage in the [Tests/AssemblyLoader.Tests/AssemblyLoaderTests.cs](Tests/AssemblyLoader.Tests/AssemblyLoaderTests.cs) file.
Note that you have to run the tests one-by-one otherwise they will conflict with each other and they might fail.
