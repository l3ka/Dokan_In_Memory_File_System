using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Total lines of code(in Visual Studio Solution) 608
/// </summary>
namespace Lab_2_oos
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LekachFileSystem lekachFileSystem = new LekachFileSystem();
                lekachFileSystem.Mount("d:\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);
                Console.WriteLine(@"Success");
            }
            catch (DokanException ex)
            {
                Console.WriteLine(@"Error: " + ex.Message);
            }
        }
    }
}
