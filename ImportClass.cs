using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Model;
using DAO;

namespace Service
{
    class ImportClass
    {
        public string dataDirectory = "DataTilInitial-Import";
        public void initialImport()
        {
            
            importRuter(dataDirectory + "/DISTRIKTER1-5-2013CSV-UTF-8.csv");
            importRuter(dataDirectory + "/DISTRIKTER6-8-2013CSV-UTF-8.csv");
            importRuter(dataDirectory + "/DISTRIKTER9-11-2013CSV-UTF-8.csv");
            importRuter(dataDirectory + "/DISTRIKTER12-14-2013CSV-UTF-8.csv");
            importRuter(dataDirectory + "/DISTRIKTER15-18-2013CSV-UTF-8.csv");
            importRuter(dataDirectory + "/DISTRIKTER19-21-2013CSV-UTF-8.csv");
            importRuter(dataDirectory + "/DISTRIKTER22-25-2013CSV-UTF-8.csv");

            importMuligeIndsamlere("DataTilInitial-Import/Muligeindsamlere-2013-UTF-8.csv");
        }

        void importMuligeIndsamlere(string sti)
        {
            StreamReader sr = new StreamReader(sti);
            sr.ReadLine();
            sr.ReadLine();  //Jeg bruger ikke de første to linjer til noget

            Distrikt aktueltDistrikt = null;
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                string[] words = line.Split(';');
                foreach (string word in words)
                {
                    word.Trim();
                }
                if (words[0] == "")
                {
                    //do nothing
                }
                else if (words[0].ToLower().StartsWith("distrikt"))
                {

                    Console.WriteLine("Distrikt: " + words[0]);
                    string[] dInfo = words[0].Split(' ');

                    string stringId = dInfo[1].Trim();
                    int i = 1;
                    while (stringId == "")
                    {
                        stringId = dInfo[i];
                        i++;
                    }
                    Console.WriteLine("stringId: " + stringId);
                    int dId = 0;
                    try
                    {
                        dId = int.Parse(stringId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Parse-error: " + e.Message);
                    }
                    if (!InterimDAO.GetInstance().districtDictionary.ContainsKey(dId))
                    {
                        Distrikt nytDistrikt = new Distrikt();
                        nytDistrikt.Id = dId;
                        InterimDAO.GetInstance().addDistrikt(nytDistrikt);
                    }
                    try
                    {
                        aktueltDistrikt = InterimDAO.GetInstance().getDistrikt(dId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Distrikt-id findes ikke... bum bum bum... dId: " + dId + "\n" + e.Message);
                    }
                }

//Navne på indsamlere
                else if (words[0].Length > 0 && words[1].Length > 0)
                {
                    string[] navne = words[0].Trim().Split(',')[0].Split(' ');
                    string enavn = navne[navne.Length - 1].Trim();
                    string fnavn = navne[0].Trim();
                    if (navne.Length > 2)
                    {
                        for (int i = 1; i < navne.Length - 1; i++)
                        {
                            fnavn += " " + navne[i].Trim();
                        }
                    }
                    string iAdresse = "";
                    if (words[0].Split(',').Length > 1)
                    {
                        iAdresse = words[0].Split(',')[1].Trim(); //Ja, så hvis der står mere end adressen, kommer det hele med.
                    }
                    string iTlfNr = words[1];
                    string iKommentar = words[2].Trim();
                    Indsamler nyIndsamler = new Indsamler();
                    nyIndsamler.Fornavn = fnavn;
                    nyIndsamler.Efternavn = enavn;
                    nyIndsamler.Adresse = iAdresse;
                    nyIndsamler.PostNr = "7500";  //95% af dem på listen bor i 7500 Holstebro
                    nyIndsamler.By = "Holstebro";
                    nyIndsamler.TlfNr = iTlfNr;   // - resten må rettes til efterfølgende
                    nyIndsamler.Kommentar = iKommentar;

                    if (aktueltDistrikt != null)
                    {
                        aktueltDistrikt.AddIndsamler(nyIndsamler); //Hvilket vi har vedtaget, betyder en indsamler der har ønsket dette distrikt
                    }
                    InterimDAO.GetInstance().addIndsamler(nyIndsamler);
                }
            }
            Console.ReadLine();
            sr.Close();
        }

        void importRuter(string sti)
        {
            StreamReader sr = new StreamReader(sti);

            Distrikt aktueltDistrikt = new Distrikt();
            Distriktleder aktuelDistriktsleder = new Distriktleder();
            Rute aktuelRute = new Rute();
            Indsamler aktuelIndsamler = new Indsamler();

            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                string[] words = line.Split(';');
                foreach (string word in words)
                {
                    word.Trim();
                }
                //Distriktsleder--------------------------------------------------------
                if (words[0].ToLower().StartsWith("distriktsleder") || words[0].ToLower().StartsWith("distriktleder"))
                {
                    string[] navne = words[1].Split(' ');
                    string enavn = navne[navne.Length - 1];
                    string fnavn = navne[0];
                    if (navne.Length > 2)
                    {
                        for (int i = 1; i < navne.Length - 1; i++)
                        {
                            fnavn += " " + navne[i];
                        }
                    }

                    aktuelDistriktsleder.Fornavn = fnavn;
                    aktuelDistriktsleder.Efternavn = enavn;
                    aktuelDistriktsleder.TlfNr = words[2];
                    aktuelDistriktsleder.AddDistrikt(aktueltDistrikt);
                    aktueltDistrikt.Distriktleder = aktuelDistriktsleder;
                    InterimDAO.GetInstance().addDistriktLeder(aktuelDistriktsleder);
                    Console.WriteLine("Distriktsleder: " + aktuelDistriktsleder.Fornavn);

                }

//Distrikt--------------------------------------------------------
                else if (words[0].ToLower().StartsWith("distrikt"))
                {
                    //Lander kun her hvis første ord ikke var distriktsleder

                    aktueltDistrikt = new Distrikt();
                    aktuelDistriktsleder = new Distriktleder();
                    int dId = 0;
                    Match match = Regex.Match(words[0], @"[0-9]+", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        string regExId = match.Groups[0].Value;

                        try
                        {
                            dId = int.Parse(regExId);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine("ParseError distrikt: " + regExId);
                        }
                    }
                    aktueltDistrikt.Id = dId;

                    //Navn for distriktet:
                    string dNavn = "";
                    if (words[0].ToLower().StartsWith("distrikt"))
                    {
                        string[] distriktHeaderArray = words[0].Split(':');
                        if (distriktHeaderArray.Length == 2)
                        {
                            dNavn = distriktHeaderArray[1];
                        }
                        else
                        {
                            Match match2 = Regex.Match(words[0], @"[^0-9\- ][^0-9]+[0-9]?$");
                            if (match2.Success)
                            {
                                string regexDlId = match2.Groups[0].Value;
                                dNavn = regexDlId;
                            }
                        }

                        if (dNavn.Trim().Length > 0)
                        {
                            Console.WriteLine("Distriktsnavn: " + dNavn);
                            aktueltDistrikt.Navn = dNavn.Trim();
                        }
                        else
                        {
                            aktueltDistrikt.Navn = "Defaultnavn";
                            Console.WriteLine("Defaultnavn, words[0]: " + words[0]);
                            Console.WriteLine("words[1]: " + words[1]);
                        }

                        aktueltDistrikt.Distriktleder = aktuelDistriktsleder;
                            //Som endnu ikke har fået noget navn eller andet.
                        InterimDAO.GetInstance().addDistrikt(aktueltDistrikt);
                    }
                }

//Rute---------------------------------------------------------------------
                else if (words[0].ToLower().StartsWith("rute"))
                {
                    aktuelRute = new Rute();

                    int rId = 0;
                    Match match = Regex.Match(words[0], @"[0-9]+");
                    if (match.Success)
                    {
                        string regExId = match.Groups[0].Value;

                        try
                        {
                            rId = int.Parse(regExId);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine("ParseError: " + regExId);
                        }
                    }

                    aktuelRute.Id = rId;
                    if (DAO.InterimDAO.GetInstance().ruteDictionary.ContainsKey(rId))
                    {
                        aktuelRute = DAO.InterimDAO.GetInstance().ruteDictionary[rId];
                    }
                    else
                    {
                        aktuelRute.SetDistriktUD(aktueltDistrikt);
                        aktueltDistrikt.AddRuteUD(aktuelRute);
                        InterimDAO.GetInstance().addRute(aktuelRute);
                    }

//Tildelt indsamler
                    if (words[1].Trim().Length > 0)
                    {
                        aktuelIndsamler = new Indsamler();

                        string[] navne = words[1].Split(',')[0].Split(' ');
                        string enavn = navne[navne.Length - 1].Trim();
                        string fnavn = navne[0].Trim();
                        if (navne.Length > 2)
                        {
                            for (int i = 1; i < navne.Length - 1; i++)
                            {
                                fnavn += " " + navne[i].Trim();
                            }
                        }
                        string iAdresse = "";
                        if (words[1].Split(',').Length > 1)
                        {
                            iAdresse = words[1].Split(',')[1].Trim(); //Ja, så hvis der står mere end adressen, kommer det hele med.
                        }
                        string iTlfNr = "";
                        if (words.Length > 1)
                            iTlfNr = words[2];

                        aktuelRute.AddTildeltIndsamlerUD(aktuelIndsamler);
                        aktuelIndsamler.AddTildeltRuteUD(aktuelRute);
                        aktuelIndsamler.Fornavn = fnavn;
                        aktuelIndsamler.Efternavn = enavn;
                        aktuelIndsamler.Adresse = iAdresse;
                        aktuelIndsamler.TlfNr = iTlfNr;
                        aktuelIndsamler.PostNr = "7500";  //95% af dem på listen bor i 7500 Holstebro
                        aktuelIndsamler.By = "Holstebro";
                        InterimDAO.GetInstance().addIndsamler(aktuelIndsamler);
                        
                    }
                    if (line.StartsWith(";;;") && words.Length>3 && words[3].Length>0)  //Linjen med kommentaren
                    {
                        aktuelIndsamler.Kommentar = words[3];
                }

                }

                    
                

                    //Vejnavn + antal husstande--------------------------------------------------------
                else if (words[1].Trim().Length > 0 && words[2].Trim().Length > 0)
                {
                    Console.WriteLine(words[1] + " - " + words[2] + " husstande.");


                    try
                    {
                        int a = int.Parse(words[2].Trim());
                        aktuelRute.AntalPrHusstand.Add(a);
                        aktuelRute.Adresser.Add(words[1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: Antal husstande: " + e.Message);
                        Console.WriteLine("Flg. kunne ikke tolkes som en adresse: " + words[1] + ", " + words[2] +
                                          "(antal husstande?)");
                        Console.WriteLine(e.ToString());
                    }
                }

                //Console.ReadLine();
            }
            sr.Close();
            Console.ReadLine();
        }


    }
}
