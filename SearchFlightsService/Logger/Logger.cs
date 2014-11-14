using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.IO;

namespace SearchFlightsService.Logger
{
    public static class Logger
    {
        public static void WriteToBookLog(string message)
        {
            try
            {
                StreamWriter outfile = new StreamWriter("" + AppDomain.CurrentDomain.BaseDirectory + @"/log/book_" + DateTime.Today.ToString("yyyy-MM-dd") + ".log", true);
                {
                    outfile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message);
                }
                outfile.Close();
            }
            catch (Exception)
            { }
        }

        public static void WriteToTimeLog(string message)
        {
            try
            {
                StreamWriter outfile = new StreamWriter("" + AppDomain.CurrentDomain.BaseDirectory + @"/log/timer_" + DateTime.Today.ToString("yyyy-MM-dd") + ".log", true);
                {
                    outfile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message);
                }
                outfile.Close();
            }
            catch (Exception)
            { }
        }


        public static void WriteToLog(string message)
        {
            try
            {
                #if DEBUG
                Console.WriteLine(message);
                return;
                #endif

                StreamWriter outfile = new StreamWriter("" + AppDomain.CurrentDomain.BaseDirectory + @"/log/" + DateTime.Today.ToString("yyyy-MM-dd") + ".log", true);
                {
                    outfile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message);
                }
                outfile.Close();
            }
            catch (Exception)
            { }
        }

        public static void WriteToLog(string[] messages)
        {
            try
            {
                StreamWriter outfile = new StreamWriter("" + AppDomain.CurrentDomain.BaseDirectory + @"/log/" + DateTime.Today.ToString("yyyy-MM-dd") + ".log", true);
                {
                    outfile.WriteLine();
                    outfile.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (string message in messages)
                    {
                        #if DEBUG
                                                Console.WriteLine(message);
                                                continue;
                        #endif
                        outfile.WriteLine(message);
                    }
                }
                outfile.Close();
            }
            catch (Exception)
            { }
        }
    }
}