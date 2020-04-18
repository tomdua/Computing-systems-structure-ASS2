using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            Assembler a = new Assembler();
            //to run tests, call the "TranslateAssemblyFile" function like this:
            //string sourceFileLocation = "C:\Users\Tom\OneDrive - post.bgu.ac.il\BGU\Semester A - 2020\Computing systems structure\Ex2.3\Code\Assembly examples\ScreenExample.asm";
           // string destFileLocation = "C:/Users/Tom/OneDrive - post.bgu.ac.il/BGU/Semester A -2020/Computing systems structure/Ex2.3/Code/Assembly examples/new 4.text";
            //a.TranslateAssemblyFile(sourceFileLocation, destFileLocation);
            a.TranslateAssemblyFile(@"C:\Users\Tom\OneDrive - post.bgu.ac.il\BGU\Semester A - 2020\Computing systems structure\Ex2.3\Code\Assembler\Div.asm", @"Add.mc");
        }
    }
}
