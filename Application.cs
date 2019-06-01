using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Timers;

namespace wlpm
{
    public class Application
    {
        public delegate void executionCallback();
        
        private bool VerboseLog = false;
        private PackageManager pm;
        private ModuleManager mm;
        private string Version = "0.0.0.1"; // this is dynamically retrieved from assembly info
        private int RetryAttempt = 0;
        private List<FileSystemWatcher> Watchers = null;
        private Dictionary<string, DateTime> WatcherChangedFilesCache;

        public Application(string projectDir, string[] args, string version) 
        {
            Version = version;
            ConsoleColorChanger.SetPrimary(Console.ForegroundColor);

            if(args.Length < 1) {
                Console.WriteLine("Warcraft 3 Lua Package Manager "+Version+" (WLPM) by ScorpioT1000");
                Console.WriteLine("Arguments:");
                Console.WriteLine("  install <package> [<version>]");
                Console.WriteLine("  - adds a new package to your package file and installs dependencies. Omit version to require head revision");
                Console.WriteLine("  update");
                Console.WriteLine("  - removes any package data and re-downloads it from the internet");
                Console.WriteLine("  build");
                Console.WriteLine("  - builds all downloaded modules and sources into target lua file");
                Console.WriteLine("  update build");
                Console.WriteLine("  - runs 'update' then 'build'");
                Console.WriteLine("  watch");
                Console.WriteLine("  - watches for changes of the sources and target and performs update or build");
                Console.WriteLine("Options:");
                Console.WriteLine("  --detailed");
                Console.WriteLine("  - add this option to get more detailed info about the internal processes");
                return;
            }

            if(hasArg(args, "--detailed")) {
                VerboseLog = true;
            }



            pm = new PackageManager(projectDir, VerboseLog);
            mm = new ModuleManager(pm, VerboseLog, Version);

            for(; RetryAttempt < 3; ++RetryAttempt) {
                try {
                    pm.RefreshPackages(hasArg(args, "update"), hasArg(args, "install"));
                    if(hasArg(args, "build")) {
                        mm.RebuildModules();
                        return;
                    } else if(hasArg(args, "install")) {
                        if(args.Length < 2) {
                            Console.WriteLine("install format: install url [version]");
                        } else {
                            pm.InstallDependency(args[1], args.Length > 2 ? args[2] : "*");
                        }
                        return;
                    } else if(hasArg(args, "watch")) {
                        mm.RebuildModules();
                        WatchForChanges();
                    } else if(hasArg(args, "update")) {
                        return;
                    } else {
                        Console.WriteLine("Wrong command, execute the program without arguments to get help");
                        return;
                    }
                } catch(Exception e) {
                    ConsoleColorChanger.UseWarning();
                    Console.Error.WriteLine("General Error: " + e.Message);
                    Console.Error.WriteLine("Source: " + e.Source);
                    Console.Error.WriteLine("");
                    Console.Error.WriteLine("Press any key to try again. Press CTRL+C to stop.");
                    ConsoleColorChanger.UsePrimary();
                    Console.ReadKey();
                    pm.Clear();
                    mm.Clear();
                }
                Console.Error.WriteLine("Retry attempt: " + RetryAttempt);
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
            if(WatcherChangedFilesCache != null) {
                WatcherChangedFilesCache.Clear();
            } else {
                WatcherChangedFilesCache = new Dictionary<string, DateTime>();
            }
            if(Watchers != null) {
                foreach(var w in Watchers) {
                    w.Dispose();
                }
                Watchers.Clear();
            } else {
                Watchers = new List<FileSystemWatcher>();
            }

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
            WatchTargetFile();
            PrintReadyMessage();
            Console.ReadKey();
            System.Environment.Exit(0);
        }

        // [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchDirectory(string path, string filter = "*.lua") 
        {
            string targetPath = Path.GetFullPath(path);
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = targetPath;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName;
            watcher.Filter = filter;

            watcher.Changed += OnSrcChanged;
            watcher.Created += OnSrcChanged;
            watcher.Deleted += OnSrcChanged;
            watcher.Renamed += OnSrcRenamed;

            watcher.EnableRaisingEvents = true;
            Watchers.Add(watcher);
        }

        // [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchProjectPackage() 
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = pm.ProjectDirectory;
            watcher.NotifyFilter = NotifyFilters.LastWrite
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName;

            watcher.Changed += OnPackageChanged;
            watcher.Created += OnPackageChanged;
            watcher.Deleted += OnPackageChanged;
            watcher.Renamed += OnPackageRenamed;

            watcher.EnableRaisingEvents = true;
            Watchers.Add(watcher);
        }

        // [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void WatchTargetFile() 
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(Path.Combine(pm.ProjectDirectory, pm.ProjectPackage.Target));
            watcher.NotifyFilter = NotifyFilters.LastWrite
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName;

            watcher.Changed += OnTargetChanged;
            watcher.Created += OnTargetChanged;
            watcher.Deleted += OnTargetChanged;
            watcher.Renamed += OnTargetRenamed;

            watcher.EnableRaisingEvents = true;
            Watchers.Add(watcher);
        }

        private void OnTargetChanged(object source, FileSystemEventArgs e)
        {
            if(!CheckIsFileReallyChanged(e.FullPath) || !mm.IsTargetChangedOutside() || !e.Name.EndsWith(pm.ProjectPackage.Target)) { return; }

            PrintWatcherEvent("Target", getChangeType(e.ChangeType), e.FullPath);

            mm.invokeASAP("ModuleManager.RebuildModules", () => {
                mm.RebuildModules();
                PrintReadyMessage();
            });
        }

        private void OnTargetRenamed(object source, RenamedEventArgs e)
        {
            if(!CheckIsFileReallyChanged(e.FullPath) || !mm.IsTargetChangedOutside() || !e.Name.EndsWith(pm.ProjectPackage.Target)) { return; }

            PrintWatcherEvent("Target", "renamed", e.OldFullPath, e.FullPath);

            mm.invokeASAP("ModuleManager.RebuildModules", () => {
                mm.RebuildModules(() => {
                    PrintReadyMessage();
                });
            });
        }

        private void OnSrcChanged(object source, FileSystemEventArgs e)
        {
            if(!CheckIsFileReallyChanged(e.FullPath)) { return; }
            PrintWatcherEvent("Source", getChangeType(e.ChangeType), e.FullPath);

            mm.invokeASAP("ModuleManager.RebuildModules", () => {
                mm.RebuildModules(() => {
                    PrintReadyMessage();
                });
            });
        }

        private void OnSrcRenamed(object source, RenamedEventArgs e)
        {
            if(!CheckIsFileReallyChanged(e.FullPath)) { return; }
            PrintWatcherEvent("Source", getChangeType(e.ChangeType), e.FullPath);

            mm.invokeASAP("ModuleManager.RebuildModules", () => {
                mm.RebuildModules(() => {
                    PrintReadyMessage();
                });
            });
        }

        private void OnPackageChanged(object source, FileSystemEventArgs e)
        {
            if(e.Name.EndsWith(pm.ProjectPackageName)) {
                if(!CheckIsFileReallyChanged(e.FullPath)) { return; }
                PrintWatcherEvent("Package config", getChangeType(e.ChangeType), e.Name);
                if(VerboseLog) Console.WriteLine("-- Package changed: " + e.Name);

                pm.invokeASAP("PackageManager.RefreshPackages", () => {
                    pm.RefreshPackages(false);
                    mm.RebuildModules(() => {
                        PrintReadyMessage();
                    });
                });
            }
        }

        private void OnPackageRenamed(object source, RenamedEventArgs e)
        {
            if(e.Name.EndsWith(pm.ProjectPackageName)) {
                if(!CheckIsFileReallyChanged(e.FullPath)) { return; }
                PrintWatcherEvent("Package config", "renamed", e.OldName, e.Name);

                pm.invokeASAP("PackageManager.RefreshPackages", () => {
                    pm.RefreshPackages(false);
                    mm.RebuildModules();
                });
            }
        }

        private void PrintReadyMessage()
        {
            if(Watchers.Count > 0) {
                var sources = String.Join(',', pm.ProjectPackage.Sources.ToArray());
                Console.WriteLine("");
                Console.WriteLine(((new Random()).Next(100) < 50 ? "Nice!" : "Great!") + " Watching for changes:");

                ConsoleColorChanger.UseSecondary();
                Console.Write("  " + pm.ProjectPackageName);
                ConsoleColorChanger.UsePrimary();
                Console.WriteLine(" -> refresh packages");

                if(sources.Length > 0) {
                    ConsoleColorChanger.UseSecondary();
                    Console.Write("  " + sources);
                    ConsoleColorChanger.UsePrimary();
                    Console.WriteLine(" -> rebuild modules");
                }

                ConsoleColorChanger.UseSecondary();
                Console.Write("  " + pm.ProjectPackage.Target);
                ConsoleColorChanger.UsePrimary();
                Console.WriteLine(" -> rebuild modules");
            }

            Console.WriteLine("");
            ConsoleColorChanger.UseAccent();
            Console.WriteLine("Now you are free to work with your map directory. Press any key to stop.");
            ConsoleColorChanger.UsePrimary();
            Console.WriteLine("");
        }

        private void PrintWatcherEvent(string prefix, string action, string filename = "", string anotherFilename = "")
        {
            if(prefix != "") Console.Write("  "+prefix+" ");
            ConsoleColorChanger.UseAccent();
            Console.Write(action+" ");
            ConsoleColorChanger.UsePrimary();
            if(filename != "") {
                ConsoleColorChanger.UseSecondary();
                Console.Write(filename+" ");
                ConsoleColorChanger.UsePrimary();
            }
            if(anotherFilename != "") {
                Console.Write(" -> ");
                ConsoleColorChanger.UseSecondary();
                Console.Write(anotherFilename+" ");
                ConsoleColorChanger.UsePrimary();
            }
            Console.WriteLine("");
        }

        private string getChangeType(WatcherChangeTypes t) 
        {
            switch(t) {
                case WatcherChangeTypes.Created:
                    return "created";
                case WatcherChangeTypes.Deleted:
                    return "deleted";
                case WatcherChangeTypes.Changed:
                    return "changed";
                case WatcherChangeTypes.Renamed:
                    return "renamed";
                case WatcherChangeTypes.All:
                    return "changed";
            }
            return "(unknown change)";
        }

        private bool CheckIsFileReallyChanged(string fileFullPath)
        {
            bool isChanged = false;
            DateTime changedAt = File.GetLastAccessTimeUtc(fileFullPath);
            if(WatcherChangedFilesCache.ContainsKey(fileFullPath)) {
                isChanged = WatcherChangedFilesCache[fileFullPath] < changedAt;
                WatcherChangedFilesCache.Remove(fileFullPath);
            }
            WatcherChangedFilesCache.Add(fileFullPath, changedAt);
            return isChanged;
        }
        
    }
}