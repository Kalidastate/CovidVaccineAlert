using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;


namespace CovidVaccineAlert
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        private static bool found = false;
        static StringBuilder dump = new System.Text.StringBuilder();

        static void Main(string[] args)
        {
            string fileName = args[0];

            while (true)
            {
                using (StreamReader reader = new StreamReader(fileName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        //Console.WriteLine(line);

                        string date = args[1];
                        string pincode = line;

                        string url = string.Format(@"https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/calendarByPin?pincode={0}&date={1}", pincode, date);
                        //https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/calendarByPin?pincode=411007&date=08-05-2021

                        var path = @".\URL2.ps1";

                        string text = string.Format("Invoke-WebRequest \"{0}\" | Select-Object -Expand Content", url);
                        File.WriteAllText(path, text);

                        Process process = new System.Diagnostics.Process();
                        ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.FileName = "powershell.exe";
                        startInfo.Arguments = string.Format(path);
                        startInfo.Verb = "runas";
                        process.StartInfo = startInfo;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;

                        process.Start();
                        string output2 = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        try
                        {
                            Root root = JsonConvert.DeserializeObject<Root>(output2);

                            foreach (centers center in root.Centers)
                            {
                                foreach (sessions session in center.Sessions)
                                {
                                    if (session.min_age_limit  < 45 && session.available_capacity > 0)
                                    {
                                        Console.WriteLine("Address: {0}", center.address);
                                        Console.WriteLine("URL: {0}", url);
                                        dump.AppendLine(string.Format("URL: {0}\n Address: {1}\n\n", url, center.address));
                                        found = true;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: {0}", e.Message);
                            Console.WriteLine("Response: {0}", output2);
                        }

                        DateTime localDate = DateTime.Now;
                        var culture = new CultureInfo("en-US");
                        Console.WriteLine("{0}, {1:G}: checked for {2} date:{3}", localDate.ToString(culture), localDate.Kind, pincode, date);
                        Thread.Sleep(500);
                    }
                }

                if (found)
                {
                    Console.WriteLine("Address: {0}", dump.ToString());
                    while (true)
                        Console.Beep();
                }

                Console.WriteLine("Next check in 2 mins");
                Thread.Sleep(120000);
            }

        }


    }

    class Root
    {
        public IList<centers> Centers;
    }

    class centers
    {
        public string center_id;
        public string name;
        public string address;
        public string state_name;
        public string district_name;
        public string block_name;
        public string pincode;
        public string lat;
        public string longitude;
        public string from;
        public string to;
        public string fee_type;
        public IList<sessions> Sessions;
    }

    class sessions
    {
        public string session_id;
        public string date;
        public int available_capacity;
        public int min_age_limit;
        public string vaccine;
        //public string slots;
    }
    
}
