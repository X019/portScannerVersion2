using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace portScanner
{
    public class scannerV2
    {

        public static bool continueLoop = true;

        public static List<scannerInformation> scannedURLs = new List<scannerInformation>();
        public static void Main(string[] args)
        {
            while(continueLoop)
                getAllThingsToBeScanned();


            Parallel.For(0, scannedURLs.Count, i => // nested parallel for loop (keep going even if previous loop isnt done)
            {
                
                Parallel.For(1, 2000, J => // 65355 is the magic number... to test set to 2000
                {
                    new Thread(new ThreadStart(() =>
                    {
                        portCanBeConnected(i, J);
                    })).Start();
            });
            }); //but... it will wait until the content of the loop is completed.
            
            for(int i = 0; i < scannedURLs.Count; i++)
                printOutEachPort(i);
            
            continueLoop = true; //reuse this for the next part
            while(continueLoop)
             askToSave();
           
           continueLoop = true;//reuse this for the next part
           while(continueLoop)
             askToCompare();

           //
        }

      public static void printOutEachPort(int index)
      {
        Console.WriteLine("\nOPEN PORTS FOR {0} (INDEX {1}): ", scannedURLs[index].formattedIP, index);
        foreach(int x in scannedURLs[index].openPorts)
            Console.Write("{0}, ", x);
        
        Console.Write("\n");
      }

        public static void getAllThingsToBeScanned()
        {
            Console.Write("\nEnter a URL or IP to be scanned. Type 'begin' to begin scanning. Type 'end' to end the program. ");
            string? usrInput = Console.ReadLine();
            if(usrInput!.ToLower().Equals("end"))
            {
                return;
            }
            if(usrInput!.ToLower().Equals("begin"))
            {
                continueLoop = false;
                return;
            }

            scannedURLs.Add(new scannerInformation
            {
                input = usrInput,
                formattedIP = getIPAddress(usrInput)
            });

            if(scannedURLs[scannedURLs.Count - 1].formattedIP == null)
            {
                Console.WriteLine("Please input a valid URL.");
                scannedURLs.RemoveAt(scannedURLs.Count - 1);
            }

            return;
            

        }

         public static IPAddress? getIPAddress(string input) 
        {     
        //returns the IP address of the given string.
         try
         {
            Uri accessedURI = new Uri(input);
            var ip = Dns.GetHostAddresses(accessedURI.Host)[0];
           return ip;
            //this attempts to convert a URL to an IP.
         }
         catch
         {
           IPAddress? IP;
           bool isIP = IPAddress.TryParse(input, out IP); 
           if(isIP)
            return IP; //it is an IP, and we can just return it!
        
            return null;
                
         }
      }      

         static void portCanBeConnected(int indexo, int portToConnectTo)
         {
            using(TcpClient client = new TcpClient())
             {
                if(scannedURLs[indexo].formattedIP == null) return;
                var r = client.BeginConnect(scannedURLs[indexo].formattedIP!, portToConnectTo, null ,null);
                bool didConnect = r.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1)); //we dont want to wait forever to try to connect to a port! so just wait 1 second...
                if(didConnect)
                    scannedURLs[indexo].openPorts.Add(portToConnectTo);
            
            }
      }
        
        public static void askToSave()
        {
            Console.WriteLine("\nEnter in an index you would like to save. Leave blank to continue. ");
            string? input = Console.ReadLine();
            int numero;
            bool isValidNumber = Int32.TryParse(input, out numero);
            if(isValidNumber)
            {
                if(numero < scannedURLs.Count)
                {
                    Console.WriteLine("enter in a name for the file: ");
                    input = Console.ReadLine();
                    writeInfo(input!, numero);
                }

            }
            else if(String.IsNullOrEmpty(input)) continueLoop = false;
        }
        public static void writeInfo(string fileName, int index)
        {
            var path = Directory.GetCurrentDirectory() + "/" + fileName + ".json";
            
            using(StreamWriter write = new StreamWriter(path))
            {
               saveScanInfo toSave = new saveScanInfo
               {
                usrInput = scannedURLs[index].input,
                ip = scannedURLs[index].formattedIP!.ToString(),
                ports = scannedURLs[index].openPorts.ToArray()
               };

                string makeItGoToJson = JsonSerializer.Serialize(toSave);
                write.Write(makeItGoToJson);
            }
        }
    
        public static void askToCompare()
        {
            Console.WriteLine("Input file name to compare: ");
            string? fileOne = Console.ReadLine();
            var path = Directory.GetCurrentDirectory() + "/" + fileOne + ".json";
            if(!File.Exists(path))
            {
                 if(String.IsNullOrWhiteSpace(fileOne)) continueLoop = false;  
                Console.WriteLine("Enter in a valid file name.");
                return;
            }
               
              string? fileTwo = Console.ReadLine();
              path = Directory.GetCurrentDirectory() + "/" + fileOne + ".json";
              if(!File.Exists(path))
              {
                if(String.IsNullOrWhiteSpace(fileTwo)) continueLoop = false;   
                Console.WriteLine("Enter in a valid file name.");
                return;
              }

            saveScanInfo? writtenInfoOne = readScan(fileOne!);
            saveScanInfo? writtenInfoTwo = readScan(fileTwo!);
            comparison(writtenInfoOne!, writtenInfoTwo!);

        }

         public static saveScanInfo? readScan(string fileName)
        {
            var path = Directory.GetCurrentDirectory() + "/" + fileName+ ".json";
         if(!File.Exists(path)) return null;
            var pathNew = Directory.GetCurrentDirectory() + "/" + fileName + ".json";
        string scannie;
         using(StreamReader read = new StreamReader(path))
        {
            scannie = read.ReadToEnd();
            read.Close();
        }
        var scanned = JsonSerializer.Deserialize<saveScanInfo>(scannie); //unpack all the information from .json!!
        return scanned;
        
      }
        public static void comparison(saveScanInfo one, saveScanInfo two)
        {
            Console.WriteLine("\nOpen ports for {0}: {1}, \nFirst input: {2}\n", one.ip, one.ports!.Length, one.usrInput);
            foreach(int x in one.ports)
                Console.Write("{0}, ", x);
            Console.WriteLine("\nOpen ports for {0}: {1}\nFirst input: {2}\n", two.ip, two.ports!.Length, two.usrInput);
            foreach(int x in two.ports)
                Console.Write("{0}, ", x);
            int howManyMore = one.ports!.Length - two.ports!.Length;
            if(howManyMore == 0) Console.WriteLine("Both IPs have the same amount of ports");
            else if(howManyMore < 0) Console.WriteLine("{0} has {1} more ports open than {2}", one.usrInput, howManyMore, two.usrInput);
            else Console.WriteLine("{0} has {1} less ports open than {2}", one.usrInput, howManyMore, two.usrInput);
        
        }

        
    }

    public class scannerInformation
    {
        public string? input{get;set;}
        public IPAddress? formattedIP{get;set;}
        public HashSet<int> openPorts = new HashSet<int>();
    }
    public class saveScanInfo
    {
        public string? usrInput{get;set;}
        public string? ip{get;set;}
        public int[]? ports{get;set;}
    }

}