using System;
using System.Runtime.InteropServices;

namespace fec
{
    class Program
    {
        static void Main(string[] args)
        {
            new ReedSolomonBenchmark().run();
//            new ReedSolomonTest().testBigEncodeDecode();
        }



    }
}