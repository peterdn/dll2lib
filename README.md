dll2lib
=======

##Summary

Produces an import library (.lib) from a target dynamic link library (.dll).

Requires `dumpbin.exe` and `lib.exe` in `%PATH%`. Easiest to run from the Visual Studio tools command prompt.

###Usage

    dll2lib.exe <options> <dll>

    Options:

        /NOCLEAN        don't delete intermediate files

##Building

Open in Visual Studio 2012+ and hit build, or build from Visual Studio tools command prompt:

    msbuild /p:Configuration=Release
