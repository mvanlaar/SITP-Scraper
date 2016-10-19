using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Tullave_Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            string ExportDir = AppDomain.CurrentDomain.BaseDirectory + "\\Export";
            System.IO.Directory.CreateDirectory(ExportDir);
            List<RutasHorarios> RutasHorarios = new List<RutasHorarios> { };
            const string ua = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            
            Console.WriteLine("Scraping Tullava for runningtimes...");
            for (int i = 1; i < 7; i++)
            {
                // /Formularios/FrecuenciasHorarios/DetalleFrecuencias_Controlador.php?Accion=ObtenerListaSinParametro&tabla=routes&valor=1&where=agency_id
                string url = "http://www.tullave.com/Formularios/FrecuenciasHorarios/DetalleFrecuencias_Controlador.php?Accion=ObtenerListaSinParametro&tabla=routes&valor={0}&where=agency_id";
                url = url.Replace("{0}", i.ToString());
                
                string filename = i.ToString() + ".json";
                
                string fullpath = ExportDir + "\\" + filename;

                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", ua);
                    client.Headers.Add("Referer", @"http://www.tullave.com/Formularios/FrecuenciasHorarios/DetalleFrecuencias.php");
                    client.Proxy = null;
                    client.DownloadFile(url, fullpath);
                    // OK Parse the Json reponse.

                    JObject o1 = JObject.Parse(File.ReadAllText(fullpath));
                    string htmloption = (string)o1.SelectToken("['html']");
                    Regex ItemRegex = new Regex(@"<option\s+value=""(?<Value>.*?)"" >");

                    //Match match = Regex.Match(htmloption, pattern);

                    foreach (Match ItemMatch in ItemRegex.Matches(htmloption))
                    {
                        string optionvalue = ItemMatch.Groups["Value"].Value;
                        string url2 = "http://www.tullave.com/Formularios/FrecuenciasHorarios/DetalleFrecuencias_Controlador.php?Accion=Grilla&page=1&rp=500&sortname=undefined&sortorder=undefined&query={0}%7C0%7C%7C{1}%7C&qtype=";
                        url2 = url2.Replace("{0}", optionvalue);
                        url2 = url2.Replace("{1}", i.ToString());
                        string filename2 = i.ToString() + "-" + optionvalue + ".json";
                        string fullpath2 = ExportDir + "\\" + filename2;
                        using (var client2 = new WebClient())
                        {
                            client2.Headers.Add("user-agent", ua);
                            client2.Headers.Add("Referer", @"http://www.tullave.com/Formularios/FrecuenciasHorarios/DetalleFrecuencias.php");
                            client2.Proxy = null;
                            client2.DownloadFile(url2, fullpath2);

                            // PArse REsponse Json:
                            dynamic json = JValue.Parse(File.ReadAllText(fullpath2));
                            dynamic rows = JsonConvert.DeserializeObject(json.rows.ToString());
                            if (rows != null) 
                            { 
                                foreach (var item in rows)
                                {
                                    dynamic cell = JsonConvert.DeserializeObject(item.cell.ToString());
                                    RutasHorarios.Add(new RutasHorarios { Agencia = cell.Agencia, Destino = cell.Destino, Paradero = cell.Paradero, Nombre = cell.Nombre, HoraLlegada = cell.HoraLlegada });
                                }
                            }
                        }
                    }
                }

            }
            // Write Output to csv
            string exportfreqfile = ExportDir + "\\RutasHorarios.txt";
            Console.WriteLine("Creating Export File RutasHorarios.txt ...");
            using (var exportfreq = new StreamWriter(exportfreqfile))
            {
                // Route record
                var csvroutes = new CsvWriter(exportfreq);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                csvroutes.Configuration.QuoteNoFields = true;
                // header 
                csvroutes.WriteField("Agenica");
                csvroutes.WriteField("Nombre");
                csvroutes.WriteField("Desticon");
                csvroutes.WriteField("Paradero");
                csvroutes.WriteField("HoraLlegada");
                csvroutes.NextRecord();
                for (int i = 0; i < RutasHorarios.Count; i++) // Loop through List with for)
                {
                    csvroutes.WriteField(RutasHorarios[i].Agencia);
                    csvroutes.WriteField(RutasHorarios[i].Nombre);
                    csvroutes.WriteField(RutasHorarios[i].Destino);
                    csvroutes.WriteField(RutasHorarios[i].Paradero);
                    csvroutes.WriteField(RutasHorarios[i].HoraLlegada);
                    csvroutes.NextRecord();
                }

            }

        }
    }
    public class RutasHorarios
    {
        public string Agencia;
        public string Nombre;
        public string Destino;
        public string Paradero;
        public string HoraLlegada;       

    }
}
