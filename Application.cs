using System;
using System.IO;
using System.Security.Permissions;

namespace wlpm
{
    public class Application
    {
        private bool VerboseLog = false;
        private PackageManager pm;
        private ModuleManager mm;
        private string Version = "0.1-beta";

        public Application(string projectDir, string[] args) 
        {
            if(args.Length < 1) {
                Console.WriteLine("Arguments:");
                Console.WriteLine("  install <package> <version>");
                Console.WriteLine("    - adds a new package to your package file and installs dependencies");
                Console.WriteLine("  build");
                Console.WriteLine("    - builds all downloaded modules into target lua file");
                Console.WriteLine("  update");
                Console.WriteLine("    - removes any package data and re-downloads it from the internet");
                Console.WriteLine("  watch");
                Console.WriteLine("    - watches for changes of the sources and target and performs build/update");
                Console.WriteLine("Options:");
                Console.WriteLine("  -detailed");
                Console.WriteLine("    - add this option to get more detailed info about how it is running");
                return;
            }

            if(hasArg(args, "-detailed")) {
                VerboseLog = true;
            }

            pm = new PackageManager(projectDir, VerboseLog);
            mm = new ModuleManager(pm, VerboseLog, Version);

            pm.RefreshPackages(hasArg(args, "update"), hasArg(args, "install"));
            if(hasArg(args, "build")) {
                mm.RebuildModules();
            } else if(hasArg(args, "install")) {
                if(args.Length < 2) {
                    Console.WriteLine("install format: install url [version]");
                } else {
                    pm.InstallDependency(args[1], args.Length > 2 ? args[2] : "*");
                }
            } else if(hasArg(args, "watch")) {
                mm.RebuildModules();
                WatchForChanges();
            } else if(hasArg(args, "update")) {
                // update is done already
            } else {
                Console.WriteLine("Wrong command, execute the program without arguments to get help");
            }
        }

        private bool hasArg(string[] args, string key)
        {
            foreach(string arg in args) {
                if(arg == key) { return true; }
            }
            return false;
        }

        private void WatchForChanges()
        {
            try {
                foreach(string d in pm.ProjectPackage.Sources) {
                    if(Path.IsPathRooted(d)) {
                        if(VerboseLog) Console.WriteLine("-- Watching "+d);
                        WatchDirectory(d);
                    } else {
                        WatchDirectory(Path.Combine(pm.ProjectDirectory, d));
                        if(VerboseLog) Console.WriteLine("-- Watching "+d);
                    }
                }
                WatchProjectPackage();
                Console.WriteLine("Watching for file changes. Press any key to exit.");
                Console.ReadKey();
            } catch(Exception e) {
                Console.WriteLine("Error: "+e.Message);
            }
        }

        // [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchDirectory(string path, string filter = "*.lua") 
        {
            string targetPath = Path.GetFullPath(path);
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = targetPath;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName;
            watcher.Filter = filter;

            watcher.Changed += OnSrcChanged;
            watcher.Created += OnSrcChanged;
            watcher.Deleted += OnSrcChanged;
            watcher.Renamed += OnSrcRenamed;

            watcher.EnableRaisingEvents = true;
                
        }

        // [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchProjectPackage() 
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = pm.ProjectDirectory;
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.FileName;

            watcher.Changed += OnPackageChanged;
            watcher.Created += OnPackageChanged;
            watcher.Deleted += OnPackageChanged;
            watcher.Renamed += OnPackageRenamed;

            watcher.EnableRaisingEvents = true;
                
        }

        private void OnSrcChanged(object source, FileSystemEventArgs e)
        {
            if(VerboseLog) Console.WriteLine("-- Src changed: " + e.Name);

            Console.WriteLine($"{getChangeType(e.ChangeType)}: {e.FullPath}");

            mm.invokeASAP("ModuleManager.RebuildModules", () => {
                mm.RebuildModules();
            });
        }

        private void OnSrcRenamed(object source, RenamedEventArgs e)
        {
            if(VerboseLog) Console.WriteLine("-- Src renamed: " + e.Name);

            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");

            mm.invokeASAP("ModuleManager.RebuildModules", () => {
                mm.RebuildModules();
            });
        }

        private void OnPackageChanged(object source, FileSystemEventArgs e)
        {
            if(e.Name.EndsWith(pm.ProjectPackageName)) {
                if(VerboseLog) Console.WriteLine("-- Package changed: " + e.Name);

                pm.invokeASAP("PackageManager.RefreshPackages", () => {
                    pm.RefreshPackages(false);
                });
            }
        }

        private void OnPackageRenamed(object source, RenamedEventArgs e)
        {
            if(e.Name.EndsWith(pm.ProjectPackageName)) {
                if(VerboseLog) Console.WriteLine("-- Package renamed: " + e.Name);

                pm.invokeASAP("PackageManager.RefreshPackages", () => {
                    pm.RefreshPackages(false);
                });
            }
        }

        private string getChangeType(WatcherChangeTypes t) 
        {
            switch(t) {
                case WatcherChangeTypes.Created:
                    return "Created";
                case WatcherChangeTypes.Deleted:
                    return "Deleted";
                case WatcherChangeTypes.Changed:
                    return "Changed";
                case WatcherChangeTypes.Renamed:
                    return "Renamed";
                case WatcherChangeTypes.All:
                    return "Multiple";
            }
            return "Unknown";
        }
        
    }
}