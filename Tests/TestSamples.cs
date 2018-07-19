using System.Collections.Generic;

namespace Tests
{
    internal static class TestSamples
    {
        public static string GetSample1()
        {
            return @"using System;
using System.Collections;
using System.Linq;
using System.Text;
 
namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
            } catch (Exception e){
                /* comment 1*/
                Console.WriteLine(1);
                // comment 2
                Console.WriteLine(2);
            }
        }
        static void Main2(string[] args)
        {
            try {
            } catch (Exception e){
                /* comment 1*/
                // comment 2
            }
        }
        static void Main3(string[] args)
        {
            try {
            } catch (Exception){
            }
        }
        static void Main4(string[] args)
        {
            try {
            } catch {
            }
        }
    }
}";
        }

        public static string GetSample1Expected()
        {
            return @"using System;
using System.Collections;
using System.Linq;
using System.Text;
 
namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
            } catch (Exception e){
                /* comment 1*/
                Console.WriteLine(1);
                // comment 2
                Console.WriteLine(2);
            }
        }
        static void Main2(string[] args)
        {
            try {
            } catch (Exception e){
    /*AutoCorrection*/
    ExceptionReporter.Report(e);                /* comment 1*/
                                                // comment 2
}
        }
        static void Main3(string[] args)
        {
            try {
            } catch (Exception exception){
    /*AutoCorrection*/
    ExceptionReporter.Report(exception);
}
        }
        static void Main4(string[] args)
        {
            try {
            } catch (System.Exception exception){
    /*AutoCorrection*/
    ExceptionReporter.Report(exception);
}
        }
    }
}";
        }
    }
}