﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Nimrod
{
    /// <summary>
    /// Responsible for accessing the file system
    /// </summary>
    public class IoOperations
    {
        public string OutputFolderPath { get; }
        public IFileSystem FileSystem { get; }
        public ILogger Logger { get; }

        public IoOperations(IFileSystem fileSystem, string outputFolderPath, ILogger logger)
        {
            this.FileSystem = fileSystem.ThrowIfNull(nameof(fileSystem));
            this.OutputFolderPath = outputFolderPath;
            this.Logger = logger.ThrowIfNull(nameof(logger));

            this.WriteLog("outputFolderPath = " + outputFolderPath);
        }

        public void WriteLog(string log)
        {
            this.Logger.WriteLine(log);
        }

        /// <summary>
        /// Take a list of files, and write in the output folder.
        /// Overwrites any existing files.
        /// </summary>
        /// <param name="files"></param>
        public void Dump(IList<FileToWrite> files)
        {
            this.RecreateOutputFolder(this.OutputFolderPath);
            this.WriteLog($"Writing {files.Count} files...");
            files.AsDebugFriendlyParallel().ForAll(content =>
            {
                this.WriteLog($"Writing {content.FileName}...");

                var filePath = this.FileSystem.Path.Combine(this.OutputFolderPath, content.FileName);
                this.FileSystem.File.WriteAllText(filePath, content.Content);
            });
            this.WriteLog($"Writing {files.Count} files...Done!");
        }

        public IEnumerable<FileInfoBase> GetFileInfos(IEnumerable<string> filePaths)
            => filePaths.Select(filePath =>
                {
                    this.WriteLog($"Reading dll {filePath}...");
                    if (this.FileSystem.File.Exists(filePath))
                    {
                        return new
                        {
                            File = this.FileSystem.FileInfo.FromFileName(filePath),
                            Success = true
                        };
                    }
                    else
                    {
                        this.WriteLog($"Warning! The specified file {filePath} doesn't exist and will be skipped.");
                        return new
                        {
                            File = null as FileInfoBase,
                            Success = false
                        };
                    }
                })
                .Where(file => file.Success)
                .Select(file => file.File);

        /// <summary>
        /// Load all assemblies found in the same folder as the given DLL
        /// </summary>
        /// <param name="files"></param>
        public void LoadAssemblies(IEnumerable<FileInfoBase> files)
        {
            var directories = files.Select(f => f.DirectoryName).Distinct();
            AssemblyLocator.Init();

            var assemblyPaths = directories.SelectMany(directory =>
                this.FileSystem.Directory.EnumerateFiles(directory, "*.dll")
                    .Select(assemblyFile => this.FileSystem.Path.Combine(directory, assemblyFile))
            ).ToList();

            // this step is time consuming (around 1 second for 100 assemblies)
            // the parallelization doesn't seems to help much
            assemblyPaths.AsDebugFriendlyParallel().ForAll(assemblyPath =>
            {
                this.WriteLog($"Trying to load assembly {assemblyPath}...");
                var assembly = Assembly.LoadFile(assemblyPath);
                this.WriteLog($"Loaded {assembly.FullName}");
            });
        }

        private void RecreateOutputFolder(string folder)
        {
            this.WriteLog($"Recursive deletion of {folder}...");

            if (this.FileSystem.Directory.Exists(folder))
            {
                this.FileSystem.Directory.Delete(folder, true);
            }
            this.FileSystem.Directory.CreateDirectory(folder);
        }
    }
}
