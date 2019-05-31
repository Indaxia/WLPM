using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using wlpm.Repository;

namespace wlpm
{
    public class PackageManager: DelayedBusyState
    {
        public Package ProjectPackage = null;
        public string ProjectPackageName = "wlpm-package.json";
        public string ProjectDirectory = "";
        public Dictionary<string, PackageDependency> Dependencies;

        private RepositoryManager rm;
        private string ProjectStateLockName = "state.lock.json";
        private string ProjectPackageDir = ".wlpm";
        private bool VerboseLog = false;

        public PackageManager(string projectDir, bool verboseLog)
        {
            ProjectDirectory = projectDir;
            Dependencies = new Dictionary<string, PackageDependency>();
            VerboseLog = verboseLog;
            rm = new RepositoryManager();
        }

        public string GetDependencyDir(PackageDependency dep)
        {
            return Path.Combine(ProjectDirectory, ProjectPackageDir, "packages", dep.id);
        }

        public string GetDependencyFile(PackageDependency dep)
        {
            return Path.Combine(ProjectDirectory, ProjectPackageDir, "packages", dep.id, "src", "file.lua");
        }

        public void InstallDependency(string url, string version)
        {
            string id = PackageDependency.generateId(url, version);

            if(Dependencies.ContainsKey(id)) {
                Console.WriteLine("This dependency already exists");
            } else {
                string packageConfigPath = Path.Combine(ProjectDirectory, ProjectPackageName);
                ArrayList lines = new ArrayList();
                StreamReader rdr = new StreamReader(packageConfigPath);
                string line;
                bool found = false;
                while ((line = rdr.ReadLine()) != null) {
                    lines.Add(line);
                    if(line.Contains("\"dependencies\":")) {
                        found = true;
                        lines.Add("        \"" + url + "\": \"" + ((version != "*" && version.Length > 0) ? version : "*") + "\",");
                    }
                }
                rdr.Close();
                StreamWriter wrtr = new StreamWriter(packageConfigPath);
                if(!found) {
                    lines.RemoveAt(0);
                    lines.Insert(0, "{");
                    lines.Insert(0, "    \"dependencies\": {");
                    lines.Insert(0, "        \"" + url + "\": \"" + ((version != "*" && version.Length > 0) ? version : "*") + "\"");
                    lines.Insert(0, "    },");
                }
                foreach (string strNewLine in lines) wrtr.WriteLine(strNewLine);
                wrtr.Close(); 

                RefreshPackages(true);
            }
        }

        public void RefreshPackages(bool loadAgain = false, bool neverLoadAgain = false)
        {
            isBusy = true;
            if(loadAgain) {
                Console.WriteLine("================= Refreshing Dependencies =================");
            } else {
                Console.WriteLine("================= Locating Dependencies =================");
            }

            List<PackageDependency> oldPackageDeps = new List<PackageDependency>();

            if(ProjectPackage != null) {
                oldPackageDeps = ProjectPackage.Dependencies;
            }

            ProjectPackage = FindProjectPackage();

            refreshPackageDir();
            
            if(VerboseLog) Console.WriteLine("-- Loading state lock file");
            JObject stateLock = loadStateLock();

            if(!loadAgain) {
                if(stateLock == null) {
                    loadAgain = true;
                } else if(stateLock.Type == JTokenType.Object 
                    && stateLock["dependencies"] != null 
                    && stateLock["dependencies"].Type == JTokenType.Object
                ) {
                    var newDeps = stateLock["dependencies"];
                    Dependencies.Clear();
                    foreach(JProperty d in newDeps) {
                        PackageDependency dep = new PackageDependency(d, ProjectStateLockName);
                        Dependencies.Add(dep.id, dep);
                    }
                    foreach(PackageDependency oldDep in oldPackageDeps) {
                        if(newDeps[oldDep.Resource] == null) {
                            loadAgain = true; // TODO: remove required dependencies only
                            break;
                        }
                    }
                }

                if(!loadAgain) {
                    foreach(PackageDependency d in ProjectPackage.Dependencies) {
                        if(Dependencies.ContainsKey(d.id)) {
                            continue;
                        } else {
                            Console.WriteLine("New dependency found: "+d.Resource+" "+d.Version);
                            loadAgain = true; // TODO: download required dependencies only
                            break;
                        }
                    }
                }
            }

            if(loadAgain && !neverLoadAgain) {
                Dependencies.Clear();
                UpdatePackages();
            }

            Console.WriteLine("================= Refreshing DONE =================");
            isBusy = false;
        }

        private void UpdatePackages()
        {
            string stateLockPath = Path.Combine(ProjectDirectory, ProjectPackageDir, ProjectStateLockName);
            string tmpPath = Path.Combine(ProjectDirectory, ProjectPackageDir, "tmp");
            if(Directory.Exists(tmpPath)) {
                Directory.Delete(tmpPath, true);
            }
            Directory.CreateDirectory(tmpPath);

            Dependencies.Clear();

            foreach(PackageDependency dep in ProjectPackage.Dependencies) {
                LoadDependency(dep);
            }

            JObject jsonDeps = new JObject();
            foreach(KeyValuePair<string, PackageDependency> kv in Dependencies) {
                jsonDeps.Add(kv.Value.toJson(true));
            }
            JObject jsonObj = new JObject(new JProperty("dependencies", jsonDeps));
            File.WriteAllText(stateLockPath, jsonObj.ToString());
        }

        private void LoadDependency(PackageDependency dep, int depth = 0)
        {
            if(depth > 512) {
                throw new PackageException("Dependency loop detected");
            }

            string tmpRoot = Path.Combine(ProjectDirectory, ProjectPackageDir, "tmp");
            if(Directory.Exists(tmpRoot)) {
                Directory.Delete(tmpRoot, true);
            }
            Directory.CreateDirectory(tmpRoot);

            Package p = DownloadDependency(dep, tmpRoot);
            foreach(PackageDependency d in p.Dependencies) {
                if(!d.sameAs(dep)) {
                    LoadDependency(d, ++depth);
                }
            }
            dep.Sources = p.Sources;
            Dependencies.Add(dep.id, dep);
        }

        private Package DownloadDependency(PackageDependency dep, string tmpRoot)
        {
            string dirPath = Path.Combine(ProjectDirectory, ProjectPackageDir, "packages", dep.id);
            var provider = rm.getProvider(dep.Resource);

            if(Directory.Exists(dirPath)) {
                Directory.Delete(dirPath, true);
            }

            Console.WriteLine("Downloading "+(dep.Type == DependencyType.File ? "file" : "repository")+ ": " + dep.Resource);

            if(dep.Type == DependencyType.File) {
                string srcPath = Path.Combine(dirPath, "src");
                string filePath = Path.Combine(srcPath, "file.lua");
                var host = (new Uri(dep.Resource)).Host;

                Directory.CreateDirectory(dirPath);
                Directory.CreateDirectory(srcPath);

                if(dep.Resource != "" && (provider != null || ProjectPackage.AllowHosts.Contains(host))) {
                    Task.WaitAll(Downloader.downloadFileAsync(dep.Resource, filePath));

                    var result = new Package();

                    result.Sources.Add("src");
                    result.Title = Path.GetFileNameWithoutExtension(dep.Resource);

                    return result;
                } else {
                    throw new PackageException("Cannot resolve package: " + dep.Resource + ", wrong URL host for file type: '" + dep.Resource + "'");
                }
            } else if(provider != null) {
                string tmpFilePath = Path.Combine(tmpRoot, "wlpm-repository.zip");
                string zipFileUrl = provider.GetZipFileUrl(dep.Resource, dep.Version);

                Task.WaitAll(Downloader.downloadFileAsync(zipFileUrl, tmpFilePath));
                if(VerboseLog) Console.WriteLine("-- From " + zipFileUrl);
                if(VerboseLog) Console.WriteLine("-- Unzipping");
                Unzipper.unzipFile(tmpFilePath, tmpRoot, dirPath);

                EmptyDir(new DirectoryInfo(tmpRoot));

                string packageConfigPath = Path.Combine(dirPath, ProjectPackageName);

                if(! File.Exists(packageConfigPath)) {
                    throw new PackageException("Cannot resolve package: " + dep.Resource + ", file '" + ProjectPackageName + "' not found inside");
                }
                return new Package(File.ReadAllText(packageConfigPath));
            }
            throw new PackageException("Cannot resolve package: " + dep.Resource + ", no suitable repository provider for this url");
        }

        private Package FindProjectPackage()
        {
            Console.Write("Locating " + ProjectPackageName + " ... ");
            string packageConfigPath = Path.Combine(ProjectDirectory, ProjectPackageName);
            if(! File.Exists(packageConfigPath)) {
                Console.Write("creating a new file ... ");
                File.WriteAllText(packageConfigPath, Package.getDefaultConfiguration());
            } else {
                Console.Write("found, reading ... ");
            }

            string jsonStr = "";
            for (int i=1; i <= 3; ++i) {
                try {
                    jsonStr = File.ReadAllText(packageConfigPath);
                    break;
                } catch (IOException) when (i <= 3) { Thread.Sleep(1000); }
            }
            
            Console.WriteLine("parsing");
            return new Package(jsonStr);
        }

        private void refreshPackageDir()
        {
            if(VerboseLog) Console.WriteLine("-- Refreshing package dir");
            string packageDirPath = Path.Combine(ProjectDirectory, ProjectPackageDir);
            if(! Directory.Exists(packageDirPath)) {
                Directory.CreateDirectory(packageDirPath);
            }

            string packagesSubdir = Path.Combine(packageDirPath, "packages");

            if(! Directory.Exists(packagesSubdir)) {
                if(VerboseLog) Console.WriteLine("-- creating "+packagesSubdir);
                Directory.CreateDirectory(packagesSubdir);
            }
        }

        private JObject loadStateLock()
        {
            string stateLockPath = Path.Combine(ProjectDirectory, ProjectPackageDir, ProjectStateLockName);
            if(! File.Exists(stateLockPath)) {
                return null;
            }
            string stateLockStr = File.ReadAllText(stateLockPath);

            return JObject.Parse(stateLockStr);
        }

        public static void EmptyDir(DirectoryInfo directory)
        {
            foreach(FileInfo file in directory.GetFiles()) file.Delete();
            foreach(DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }
    }
}