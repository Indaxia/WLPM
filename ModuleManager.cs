using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace wlpm
{
    public class ModuleManager: DelayedBusyState
    {
        public delegate void executionCallback();

        private DateTime targetLastChange;

        private bool VerboseLog = false;
        private PackageManager pm;
        private string ClientScriptStart = "-- (wlpm-generated-code start)";
        private string ClientScriptEnd = "-- (wlpm-generated-code end)\r\r";
        private string AppVersion;

        public ModuleManager(PackageManager _pm, bool verboseLog, string appVersion)
        {
            pm = _pm;
            VerboseLog = verboseLog;
            AppVersion = appVersion;
            Clear();
        }

        public void RebuildModules(executionCallback onSuccess = null)
        {
            pm.invokeASAP("ModuleManager.RebuildModules", () => {
              _RebuildModules();
              if(onSuccess != null) onSuccess();
            });
        }

        public void Clear()
        {
            targetLastChange = DateTime.UtcNow;
            targetLastChange.AddDays(-1);
        }

        public bool IsTargetChangedOutside()
        {
            string targetFilename = Path.Combine(pm.ProjectDirectory, pm.ProjectPackage.Target);
            DateTime dt = File.GetLastWriteTimeUtc(targetFilename);
            return dt.CompareTo(targetLastChange) != 0;
        }

        private void _RebuildModules()
        {
            isBusy = true;
            
            ConsoleColorChanger.UseAccent();
            Console.WriteLine("Rebuilding modules");
            ConsoleColorChanger.UsePrimary();

            string targetFilename = Path.Combine(pm.ProjectDirectory, pm.ProjectPackage.Target);
            string targetOriginal = "";

            for (int i=1; i <= 30; ++i) {
                try {
                    targetOriginal = File.ReadAllText(targetFilename);
                    break;
                } catch (IOException) when (i <= 30) { Thread.Sleep(200); }
            }


            targetOriginal = RemoveBetween(targetOriginal, ClientScriptStart, ClientScriptEnd);

            string targetHeader = "";
            string targetTop = ""; 
            string targetBottom = "\r\r";

            targetHeader += "\r\r-- Warcraft 3 Lua Package Manager " + AppVersion;
            targetHeader += "\r-- Build time: " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss zzz");
            if(pm.ProjectPackage.InsertModuleLoader) {
              Console.Write("  Code of ");
              ConsoleColorChanger.UseSecondary();
              Console.Write("WLPM Module Manager");
              ConsoleColorChanger.UsePrimary();
              Console.WriteLine(@" added to the header. To disable, set ""insertModuleLoader"" to false in your " + pm.ProjectPackageName);
              targetHeader += "\r"+GetClientScript();
            } else {
              Console.Write("Code of ");
              ConsoleColorChanger.UseSecondary();
              Console.Write("WLPM Module Manager");
              ConsoleColorChanger.UsePrimary();
              Console.WriteLine(" is skipped according to your " + pm.ProjectPackageName);

            }
            targetHeader += "\r\r";

            foreach(string index in pm.DependenciesOrderIndex) {
                if(! pm.Dependencies.ContainsKey(index)) {
                  throw new ModuleException("Dependencies collection has no index '"+index+"' but it's stored in indexes. Try to run 'wlpm update'");
                }
                PackageDependency dep = pm.Dependencies[index];
                string code = GetCodeFor(dep);
                if(dep.TopOrder) {
                    targetTop += "\r\r" + code;
                } else {
                    targetBottom += "\r\r" + code;
                }
            }

            string target = ClientScriptStart + targetHeader + targetTop + targetBottom + ClientScriptEnd + targetOriginal;

            for (int i=1; i <= 30; ++i) {
                try {
                    File.WriteAllText(targetFilename, target);
                    break;
                } catch (IOException) when (i <= 30) { Thread.Sleep(200); }
            }

            UnsubscribeASAPEvent("ModuleManager.RebuildModules");

            targetLastChange = File.GetLastWriteTimeUtc(targetFilename);

            isBusy = false;
        }

        private string GetCodeFor(PackageDependency dep)
        {
            Console.Write("  Building ");
            ConsoleColorChanger.UseSecondary();
            Console.WriteLine(dep.Resource);
            ConsoleColorChanger.UsePrimary();

            var dirs = new List<string>();
            string result = GetClientScriptTagStart(dep.Resource);

            if(dep.Type == DependencyType.Package) {
                foreach(var src in dep.Sources) {
                    if(VerboseLog) Console.WriteLine("-- Generating code for source: "+src);
                    dirs.Add(pm.GetDependencyDir(dep));
                }
                
                var filenames = new List<string>();
                foreach(var dir in dirs) {
                    filenames.AddRange(Directory.GetFiles(dir, "*.lua", SearchOption.AllDirectories));
                }

                foreach(var filename in filenames) {
                    if(VerboseLog) Console.WriteLine("-- Loading code from: "+filename);
                    result += "\r\r" + File.ReadAllText(filename);
                }
            } else {
                result += File.ReadAllText(pm.GetDependencyFile(dep));
            }

            return result + GetClientScriptTagEnd(dep.Resource);
        }

        private string RemoveBetween(string source, string start, string end)
        {
            var starts = source.IndexOf(start);
            if(starts == -1) {
                return source;
            }
            var ends = source.IndexOf(end) + end.Length;
            if(ends == -1) {
                throw new ModuleException("  Cannot clean target file: end tag not found ("+end+")");
            }
            if(starts < 2) {
                return source.Substring(ends);
            }
            return source.Substring(0, starts - 1) + source.Substring(ends);
        }

        private string GetClientScriptTagStart(string id)
        {
            return "-- (wlpm-start) " + id + "\r";
        }

        private string GetClientScriptTagEnd(string id)
        {
            return "\r-- (wlpm-end) " + id;
        }

        private string GetClientScript() // TODO: import from the solution dir
        {
            return 
@"-- Module Manager
-- author: ScorpioT1000 / scorpiot1000@yandex.ru / github.com/indaxia/WLPM

local wlpmModules = {}

function wlpmDeclareModule(name, dependenciesOrContext, context)
  local theModule = {
    loaded = false,
    dependencies = {},
    context = nil,
    exports = {},
    exportDefault = nil
  }
  if (type(context) == ""function"") then
    theModule.context = context
    if (type(dependenciesOrContext) == ""table"") then
      theModule.dependencies = dependenciesOrContext
    end
  elseif (type(dependenciesOrContext) == ""function"") then
    theModule.context = dependenciesOrContext
  else  
    print(""WLPM Error: wrong module declaration: '"" .. name .. ""'. Module requires context function callback."")
    return
  end
  wlpmModules[name] = theModule
end

function wlpmLoadModule(name, depth)
  local theModule = wlpmModules[name]
  if (type(depth) == 'number') then
    if (depth > 512) then
      print(""WLPM Error: dependency loop detected for the module '"" .. name .. ""'"")
      return
    end
    depth = depth + 1
  else
    local depth = 0
  end
  if (type(theModule) ~= ""table"") then
    print(""WLPM Error: module '"" .. name .. ""' not exists or not yet loaded. Call importWM at your initialization section"")
    return
  elseif (not theModule.loaded) then
    for _, dependency in ipairs(theModule.dependencies) do
      wlpmLoadModule(dependency, depth)
    end
    
    local cb_import = function(moduleOrWhatToImport, moduleToImport) -- import default or import special
      if (type(moduleToImport) ~= ""string"") then
        return wlpmImportModule(moduleOrWhatToImport)
      end
      return wlpmImportModule(moduleToImport, moduleOrWhatToImport)
    end
    local cb_export = function(whatToExport, singleValue) -- export object or key and value
      if (type(whatToExport) == ""table"") then
        for k,v in pairs(whatToExport) do theModule.exports[k] = v end -- merges exports
      elseif (type(whatToExport) == ""string"") then
        theModule.exports[whatToExport] = singleValue
      else
        print(""WLPM Error: wrong export syntax in module '"" .. name .. ""'. Use export() with a single object arg or key-value args"")
        return
      end
    end
    local cb_exportDefault = function(defaultExport) -- export default
      if (defaultExport == nil) then
        print(""WLPM Error: wrong default export syntax in module '"" .. name .. ""'. Use exportDefault() with an argument"")
        return
      end
      theModule.exportDefault = defaultExport
    end
    
    theModule.context(cb_import, cb_export, cb_exportDefault)
    theModule.loaded = true
  end
  
  return theModule
end

function wlpmImportModule(name, whatToImport)
  theModule = wlpmLoadModule(name)
  if (type(whatToImport) == ""string"") then
    if(theModule.exports[whatToImport] == nil) then
      print(""WLPM Error: name '"" .. whatToImport .. ""' was never exported by the module '"" .. name .. ""'"")
      return
    end
    return theModule.exports[whatToImport]
  end
  return theModule.exportDefault
end

function wlpmLoadAllModules()
  for name,theModule in pairs(wlpmModules) do wlpmLoadModule(name) end
end

WM = wlpmDeclareModule
importWM = wlpmImportModule
loadAllWMs = wlpmLoadAllModules -- call to disable lazy loading mechanics
";
        }
    }
}