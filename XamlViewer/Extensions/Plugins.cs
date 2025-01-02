using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace XamlViewer.Extensions;

public class Plugins
{
    public static async Task Load(ILogger logger)
    {
        var plugins = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "plugins");

        if (!Directory.Exists(plugins))
        {
            Directory.CreateDirectory(plugins);
        }
        var files = Directory.GetFiles(plugins, "*.dll");

        //Assembly.GetEntryAssembly().GetReferencedAssemblies();

        if (files.Any())
        {
            var types = new List<Type>();
            //files.ForEach(path => Assembly.LoadFile(path));

            files.ForEach(path =>
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);


                var t = assembly.GetTypes()
                 .Where(a => a.Name.EndsWith("GlobalStaticResources", StringComparison.Ordinal))
                 .ToList();

                types.AddRange(t);

            });


            //加载资源
            types.ForEach(a =>
            {
                try
                {
                    var d = Activator.CreateInstance(a);
                    var method1 = a.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                    var method2 = a.GetMethod("RegisterDefaultStyles", BindingFlags.Public | BindingFlags.Static);
                    var method3 = a.GetMethod("RegisterResourceDictionariesBySource", BindingFlags.Public | BindingFlags.Static);
                    method1?.Invoke(d, null);
                    method2?.Invoke(d, null);
                    method3?.Invoke(d, null);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "加载dll错误");
                }
            });




        }
    }
}
