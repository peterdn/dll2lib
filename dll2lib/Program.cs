using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace dll2lib
{
    static class ExtensionMethods
    {
        public static string ReadRequiredLine(this StreamReader reader)
        {
            var line = reader.ReadLine();
            if (line == null)
                throw new InvalidDataException("Unexpected end of file");
            return line;
        }
    }

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
            if (args.Length < 1) 
                return Usage();

            var dllpath = "";
            var cleanfiles = true;
            foreach (var arg in args)
            {
                if (arg.ToLower() == "/noclean")
                    cleanfiles = false;
                else if (dllpath.Length == 0)
                    dllpath = arg;
                else
                    return Usage();
            }

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
            }
            catch (Exception ex)
            {
                Console.WriteLine("RunDumpbin: " + ex.Message);
                return -1;
            }

            try
            {
                Dump2Def(dmppath, defpath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Dump2Def: " + ex.Message);
                return -1;
            }
            finally
            {
                if (cleanfiles && File.Exists(dmppath))
                    File.Delete(dmppath);
            }

            try
            {

                RunLib(defpath, libpath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RunLib: " + ex.Message);
                return -1;
            }
            finally
            {
                if (cleanfiles && File.Exists(defpath))
                    File.Delete(defpath);
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
                    // skip header
                    for (int i = 0; i < 3; ++i)
                        dmpfile.ReadRequiredLine();

                    // check input file type
                    var next = dmpfile.ReadRequiredLine().Trim();
                    if (next != "File Type: DLL")
                    {
                        throw new InvalidDataException(String.Format("Unexpected file type: {0}", next));
                    }

                    // skip info lines
                    for (int i = 0; i < 10; ++i)
                        dmpfile.ReadRequiredLine();

                    // assert next 2 lines are what we expect
                    if (!dmpfile.ReadRequiredLine().TrimStart().StartsWith("ordinal"))
                        throw new InvalidDataException("Unexpected input; expected 'ordinal'");

                    if (dmpfile.ReadRequiredLine().Trim().Length != 0)
                        throw new InvalidDataException("Unexpected input; expected empty line");

                    // begin exports
                    deffile.WriteLine("EXPORTS");
                    while (true)
                    {
                        var line = dmpfile.ReadRequiredLine();

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
                    if (!dmpfile.ReadRequiredLine().Trim().StartsWith("Summary"))
                        throw new InvalidDataException();
                }
            }
        }

        private static int Usage(string message = "")
        {
            if (message.Length > 0)
                Console.WriteLine(message);
            else
            {
                Console.WriteLine("Usage: dll2lib.exe <options> <dll>");
                Console.WriteLine();
                Console.WriteLine("  options:");
                Console.WriteLine();
                Console.WriteLine("    /NOCLEAN");
            }
            return -1;
        }
    }
}
