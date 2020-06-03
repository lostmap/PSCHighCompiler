using System;
using System.IO;

namespace PSCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            if (args.Length == 0)
            {
                throw new Exception("expected file name");
            }
            
            string ext = Path.GetExtension(args[0]);
            if (ext != ".psc")
            {
                throw new Exception("wrong file extension");
            }
            */
            try
            {
                string code = File.ReadAllText("C:/Users/Oleg/Documents/RPO/VirtualDll/Env/Tests/Example/test.psc");
                //string code = File.ReadAllText(args[0]);
                Parser parser = new Parser(code);
                Node head = parser.Parse();

                Compiler compiler = new Compiler();
                compiler.Compile(head);
                byte[] byteCode = compiler.GetByteCode();

                using (FileStream fstream = new FileStream("../../../../../../../Tests/CompilerTest/" +
                    Path.GetFileNameWithoutExtension(args[0]) + ".bpsc", FileMode.Create))
                {
                    fstream.Write(byteCode, 0, byteCode.Length);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
