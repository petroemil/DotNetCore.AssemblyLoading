using System.Reflection;

namespace Assembly2
{
    public class Main
    {
        public (string, string) CheckAssemblyVersion()
        {
            var assembly = typeof(System.Reactive.Observer).Assembly;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            var assemblyFullName = assembly.FullName;

            return (assemblyFullName, fileVersion);
        }
    }
}
