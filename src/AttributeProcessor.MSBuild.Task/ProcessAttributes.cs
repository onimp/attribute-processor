using System.Collections.Generic;
using System.IO;
using System.Linq;
using AttributeProcessor.Core;
using AttributeProcessor.Core.Extensions;
using AttributeProcessor.Core.Processors;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using ILogger = AttributeProcessor.Core.Logging.ILogger;

namespace AttributeProcessor.MSBuild.Task;

[UsedImplicitly]
public class ProcessAttributes : Microsoft.Build.Utilities.Task {

    public virtual string? AssemblyPath { get; set; }
    public virtual ITaskItem[]? AssemblySearchPaths { get; set; }

    private readonly ILogger logger;

    public ProcessAttributes() {
        logger = new TaskLoggingHelperAdapter(Log);
    }

    public override bool Execute() {
        if (AssemblyPath == null)
            return ReturnError($"\"{nameof(AssemblyPath)}\" is not set");

        if (!File.Exists(AssemblyPath))
            return ReturnError($"Assembly \"{nameof(AssemblyPath)}\" doesn't exist");

        var configuration = Configure(AssemblyPath);
        var context = CreateModuleContext();
        using (var module = ModuleDefMD.Load(configuration.OriginalAssemblyPath, context)) {
            if (GetProcessors(module).Any(processor => !processor.Process()))
                return false;

            var options = new ModuleWriterOptions(module);
            module.Write(AssemblyPath, options);
        }
        Cleanup(configuration);
        return true;
    }

    private ModuleContext CreateModuleContext() {
        var context = new ModuleContext();
        var assemblyResolver = new AssemblyResolver(context) { DefaultModuleContext = context };

        AssemblySearchPaths?
            .Select(it => it.ItemSpec)
            .ForEach(it => assemblyResolver.PostSearchPaths.Add(it));

        context.AssemblyResolver = assemblyResolver;
        context.Resolver = new Resolver(assemblyResolver);
        return context;
    }

    private Configuration Configure(string assemblyPath) {
        var assemblyPathWithoutExtension = Path.Combine(
            // ReSharper disable once AssignNullToNotNullAttribute
            Path.GetDirectoryName(assemblyPath),
            Path.GetFileNameWithoutExtension(assemblyPath)
        );
        var originalAssemblyPath = $"{assemblyPathWithoutExtension}.Original.dll";
        var originalAssemblyPdbPath = $"{assemblyPathWithoutExtension}.Original.pdb";

        File.Copy(assemblyPath, originalAssemblyPath, true);

        var assemblyPdbPath = $"{assemblyPathWithoutExtension}.pdb";
        if (File.Exists(assemblyPdbPath))
            File.Copy(assemblyPdbPath, originalAssemblyPdbPath, true);

        return new Configuration(originalAssemblyPath, originalAssemblyPdbPath);
    }

    private static void Cleanup(Configuration configuration) {
        File.Delete(configuration.OriginalAssemblyPath);
        File.Delete(configuration.OriginalPbdPath);
    }

    private List<IAttributeProcessor> GetProcessors(ModuleDefMD module) => new() {
        new ConditionalInvocationProcessor(module, logger)
    };

    private bool ReturnError(string message) {
        Log.LogError(message);
        return false;
    }

    private record Configuration(string OriginalAssemblyPath, string OriginalPbdPath);

}
