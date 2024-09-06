> [!CAUTION]
> WLPM IS DEPRECATED - USE [IMP](https://github.com/Indaxia/imp-lua)

## Warcraft 3 Lua Package Manager
A modern solution for Warcraft 3 map development!

Brings package management and es6-like Lua modules to your map project.

## Features
WLPM consists of a Package Manager and a Module Manager with it's own Lua part of code.

We introduce a new way of working with dependencies - [WLPM Module Manager](https://github.com/Indaxia/wc3-wlpm-module-manager):satellite:

### Package Manager Features
- Works with maps in a map-as-directory mode
- Own package config format in JSON
- Install [packages with dependencies](https://github.com/Indaxia/wlpm-wc3-demo-hello-user/blob/master/wlpm-package.json) from Github and Bitbucket
- Install Lua files directly from Github, Bitbucket or custom hosts in config
- File and directory watcher (war3map.lua, sources, config)
- Dependency version resolution

### Module Manager Features
- Include custom user directories as advanced sources
- Right dependency order in the target file
- ES6-like imports and exports in the Lua script
- Really fast target builder on-the-go (C# watcher)

## Download

[WLPM for Windows 10 x64](https://indaxia.com/public/releases/wlpm/0.7-beta/Install%20WLPM%20for%20Windows%2010%20x64.exe)

[WLPM for any Windows](https://indaxia.com/public/releases/wlpm/0.7-beta/Install%20WLPM%20for%20Windows.exe) (legacy)

WLPM for macOS (planned, waiting for [Warcraft 3 fixes](https://us.battle.net/forums/en/bnet/topic/20771617132))

## Quick Start

1. Install WLPM
2. Save your map as a directory (Save as... menu item)
3. Open any terminal window (press Win+R and enter "cmd")
4. enter ```cd <your map directory>```
5. then enter ```wlpm``` - it shows all applicable commands

To initialize your package enter ```wlpm update```. It will create wlpm-package.json and .wlpm directory with the dependencies. If you use git (mercurial/svn/...) add .wlpm to your ignore file (.gitignore).

To add new dependency enter ```wlpm install <package> <version>```

#### Example:
```
wlpm install https://github.com/Indaxia/wlpm-wc3-demo-hello
```
We don't recommend to use "any" version in public projects. Some scammers or stolen accs may update the code and make it malicious. 

#### Specific version example (retrieve from git tag):
```
wlpm install https://github.com/Indaxia/wlpm-wc3-demo-hello 1.1
```

Use ```wlpm watch``` to let watcher notify PM and MM if something changed and perform download new packages and/or rebuild modules.

To get help about module management refer the [MM documents](https://github.com/Indaxia/wc3-wlpm-module-manager).

## Advanced Usage

### Including files
You can include files directly (Big Integer in the example):
```
wlpm install https://raw.githubusercontent.com/DeBos99/lua-bigint/master/bigint.lua * file
```

### Disabling Module Manager (MM) script
If you don't want to use MM on the client (Lua) side you can disable it by adding a new option to your wlpm-package.json:
```
  "insertModuleLoader": false
```
With this option MM just includes code of the dependencies without the MM

### Executing a command after building
It's possible execute a terminal command when the building process finishes:
```
  "afterBuild": "echo \"hello world\""
```
It works for root projects only.

## Publishing Packages

If you want to publish your package folow these steps:
1. Create a git repository at Github or Bitbucket
2. Create wlpm-package.json in the repository root
3. Add the "dependencies" and "sources" parameters. Refer the full config example below.
4. (optional) add git tag to the repository
5. Now this is a WLPM package!

## Full config example (wlpm-package.json) 

```js
{
    "title": "WLPM - Demo Map Package", // (optional) your package or root project title
    "author": "ScorpioT1000 / scorpiot1000@yandex.ru", // (optional) author information
    "license": "MIT", // (optional) source code license
    "dependencies": { // list of packages and files required by your source code
        // github repository from a master branch
        "https://github.com/123/456": "*",
        
        // github repository from the release tagged as "1.1.1"
        "https://github.com/123/456": "1.1.1",
        
        // bitbucket repository from the release tagged as "1.0.0" in an object format
        "https://bitbucket.org/123/789": { "type": "package", "version": "1.0.0" },
        
        // inserts a file directly from the repository
        "https://github.com/123/456/blob/master/somefile.lua": { 
            "type": "file", 
            "topOrder": true  // omit this option or set to false to insert the file after repositories' sources
        }
    },
    // (optional for root project) where your sources are stored. It's important for the package, but can be omitted for root project (it watches "target")
    "sources": [
        "src"
    ],
    // (optional) where to store compiled lua build. It works for root project only.
    "target": "war3map.lua",
    // (optional) execute this command after build, e.g. "echo \"Hello!\""
    "afterBuild": "",
    // (optional) set this to false if you want to replace built-in module manager by your own
    "insertModuleLoader": true,
    // (optional) allow more hosts for direct file dependency (allows github.com and bitbucket.org by default). It works for root project only.
    "allowHosts": []
    // (optional) set custom file extensions when building "sources"
    "sourceExtensions": "*.lua"
}
```

## Restrictions

1. It doesn't support partial version placeholders like ```1.*``` because it doesn't use package registry
2. It performs full re-download on any config requirement change (planned to fix in the future)
3. No VSCode integration yet, but it's planned
4. Execution of after-build custom scripts is not implemented yet

## For C# developers

You are free to fork and build your own modifications! My requirement is that any "author" fields in C# code are allowed to be supplemented only(!).

### How to build

```
dotnet publish -c Release --self-contained --runtime win10-x64 /property:Version=VERSION_HERE
dotnet publish -c Release --self-contained --runtime win-x86 /property:Version=VERSION_HERE
```

ScorpioT1000 © 2019
