dll2lib
=======

## Summary

Produces an import library (.lib) from a target dynamic link library (.dll).

Requires `dumpbin.exe` and `lib.exe` in `%PATH%`. Easiest to run from the Visual Studio tools command prompt.

### Usage

For ease of use, a pre-built binary is provided in `dll2lib\bin\Release`, however feel free to build your own.

    dll2lib.exe <options> <dll>

    Options:

        /NOCLEAN        don't delete intermediate files

The import library file is output to the same directory as the target dll.

## Building

Open in Visual Studio 2012+ and hit build, or build from Visual Studio tools command prompt:

    msbuild /p:Configuration=Release
