using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MBasic
{
    class Program
    {
        static void Main(string[] args)
        {
            string progText = "";
            using(StreamReader sr = new StreamReader(new FileStream("prog.mb", FileMode.Open)))
	        {
		         progText = sr.ReadToEnd();
	        }
            MBasic1.Run(progText);
            
        }
    }
}
