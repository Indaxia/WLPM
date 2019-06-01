Warcraft 3 Lua Package Manager

Provides package management features and es6-like Lua modules to your Warcraft 3 Lua map project.

## Features

## Quick Start

## Advanced Usage

## Publishing Packages

## Restrictions

## For developers

### How to build

```
$version = git describe --tags --abbrev=0
dotnet publish -c Release --self-contained --runtime win10-x64 /property:Version=$version
dotnet publish -c Release --self-contained --runtime win-x86 /property:Version=$version
```