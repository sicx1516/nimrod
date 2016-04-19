﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Nimrod
{
    public class Generator
    {
        public IoOperations IoOperations { get; }
        public Generator(IoOperations ioOperations)
        {
            this.IoOperations = ioOperations.ThrowIfNull(nameof(ioOperations));
        }

        public void Generate(IEnumerable<string> dllPaths, ModuleType moduleType)
        {
            this.IoOperations.RecreateOutputFolder();

            this.IoOperations.WriteLog($"Writing static files...");
            WriteStaticFiles(moduleType);

            var assemblies = this.IoOperations.GetAssemblies(dllPaths);
            var types = this.GetTypesToWrite(assemblies).ToList();
            this.WriteTypes(types, moduleType);
        }

        private void WriteTypes(IList<Type> types, ModuleType moduleType)
        {
            this.IoOperations.WriteLog($"Writing {types.Count} files...");
            foreach (var type in types)
            {
                var content = this.GetContentText(type, moduleType);
                this.IoOperations.WriteFile(content, type.GetTypeScriptFilename());
            }
            this.IoOperations.WriteLog($"Writing {types.Count} files...Done!");
        }

        private IEnumerable<Type> GetTypesToWrite(IEnumerable<Assembly> assemblies)
        {
            this.IoOperations.WriteLog($"Discovering types..");
            var types = TypeDiscovery.GetControllerTypes(assemblies, true).ToList();
            // Write all types except the ones in System
            var toWrites = types
                .Where(t => !t.IsSystem())
                .Select(t => t.IsGenericType ? t.GetGenericTypeDefinition() : t)
                .Distinct();
            return toWrites;
        }

        /// <summary>
        /// Get content of a type script for a given type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="moduleType"></param>
        /// <returns></returns>
        private string GetContentText(Type type, ModuleType moduleType)
        {
            this.IoOperations.WriteLog($"Writing {type.Name}...");

            var buildRules = ToTypeScriptBuildRules.GetRules(moduleType);
            var toTypeScript = buildRules.GetToTypeScript(type);
            var lines = toTypeScript.GetLines();
            var indentedLines = lines.IndentLines();
            // add empty line, because it's prettier
            return string.Join(Environment.NewLine, indentedLines.Concat(new[] { "" }).ToArray());
        }


        private void WriteStaticFiles(ModuleType module)
        {
            var content = new StaticWriter().Write(module);
            this.IoOperations.WriteFile(content, "IRestApi.ts");
        }

    }
}

