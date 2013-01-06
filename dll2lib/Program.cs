using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace dll2lib
{
    class Program
    {
        private static readonly string[] PRIVATES = 
        {
            "DllCanUnloadNow",
            "DllGetClassObject",
            "DllGetClassFactoryFromClassString",
            "DllGetDocumentation",
            "DllInitialize",
            "DllInstall",
            "DllRegisterServer",
            "DllRegisterServerEx",
            "DllRegisterServerExW",
            "DllUnload",
            "DllUnregisterServer",
            "RasCustomDeleteEntryNotify",
            "RasCustomDial",
            "RasCustomDialDlg",
            "RasCustomEntryDlg"
        };

        static int Main(string[] args) 
        {
            if (args.Length != 1) 
                return Usage();

            var dllpath = args[0];
            if (!File.Exists(dllpath))
                return Usage(string.Format("Could not find input file {0}", dllpath));

            var index = dllpath.LastIndexOf('.');
            var dllname = index >= 0 ? dllpath.Substring(0, index) : dllpath;

            var dmppath = dllname + ".dmp";
            var defpath = dllname + ".def";
            var libpath = dllname + ".lib";

            try
            {
                RunDumpbin(dllpath, dmppath);
                Dump2Def(dmppath, defpath);
                RunLib(defpath, libpath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }

            return 0;
        }

        private static void RunDumpbin(string dllpath, string dmppath)
        {
            var procinfo = new ProcessStartInfo("dumpbin", string.Format("/out:{0} /exports {1}", dmppath, dllpath));
            procinfo.UseShellExecute = false;
            var dumpbin = Process.Start(procinfo);
            dumpbin.WaitForExit();
            if (dumpbin.ExitCode != 0)
                throw new ApplicationException(string.Format("dumpbin failed with exit code {0}", dumpbin.ExitCode));
        }

        private static void RunLib(string defpath, string libpath)
        {
            var procinfo = new ProcessStartInfo("lib", string.Format("/machine:arm /def:{0} /out:{1}", defpath, libpath));
            procinfo.UseShellExecute = false;
            var lib = Process.Start(procinfo);
            lib.WaitForExit();
            if (lib.ExitCode != 0)
                throw new ApplicationException(string.Format("lib failed with exit code {0}", lib.ExitCode));
        }

        private static void Dump2Def(string dmppath, string defpath)
        {
            using (var dmpfile = new StreamReader(File.OpenRead(dmppath)))
            {
                using (var deffile = new StreamWriter(File.OpenWrite(defpath)))
                {
                    // skip first 14 lines
                    for (int i = 0; i < 14; ++i)
                        dmpfile.ReadLine();

                    // assert next 2 lines are what we expect
                    if (!dmpfile.ReadLine().TrimStart().StartsWith("ordinal"))
                        throw new InvalidDataException();

                    if (dmpfile.ReadLine().Trim().Length != 0)
                        throw new InvalidDataException();

                    // begin exports
                    deffile.WriteLine("EXPORTS");
                    while (true)
                    {
                        var line = dmpfile.ReadLine().Trim();

                        if (line.Length == 0)
                            break;

                        var words = line.Split(' ');
                        var index = words.Length - 1;
                        var hasforward = words[index].EndsWith(")");
                        if (hasforward)
                            index -= 3;

                        var proc = words[index];
                        if (proc != "[NONAME]")
                        {
                            deffile.Write(proc);
                            if (PRIVATES.Contains(proc))
                                deffile.Write(" PRIVATE");
                            deffile.WriteLine();
                        }
                    }

                    // assert begin of summary
                    if (!dmpfile.ReadLine().Trim().StartsWith("Summary"))
                        throw new InvalidDataException();
                }
            }
        }

        private static int Usage(string message = "")
        {
            if (message.Length > 0)
                Console.WriteLine(message);
            Console.WriteLine("Usage: dll2lib.exe <dll>");
            return -1;
        }
    }
}
