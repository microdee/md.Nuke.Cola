# Tool Support {#ToolSupport}

[TOC]

Nuke.Cola comes with explicit support for some tools

## XMake

Automatically download a specified [XMake](https://xmake.io) version bundle into the temporary `.nuke/temp` folder if needed. This is mostly used for managing C++ packages through XRepo.

There are extra utilities for parsing the output of the XRepo command so that can be used with other C++ build tools which may not have a good method for managing C++ dependencies (*khm* Unreal Engine *khm*).

## CMake

Automatically download a specified CMake version inside the temporary `.nuke/temp` folder if needed, so we don't need to assume one installed on the system.

## VCPKG

Automatically clone the VCPKG repository inside the `.nuke/temp` folder if needed. VCPKG can be used on its own, but it is included for somewhat ensuring the meta-package-management feature of XRepo.