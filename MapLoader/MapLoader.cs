using System;
using System.Linq;
using V_MP.Server.API;
using V_MP.Server.APIStreamed;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Globalization;

namespace MapLoader
{
    public class MapLoader : APIScript
    {
        private string[] object_files;

        public string GetBetweenString(string a, string b, string c)
        {
            return c.Substring((c.IndexOf(a) + a.Length), (c.IndexOf(b) - c.IndexOf(a) - a.Length));
        }

        public static string GetIfExist(string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            int posB = value.LastIndexOf(b);
            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("Error");
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        public void LoadMapStart()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] files = Directory.GetFiles(assemblyFolder + "\\maps")
                              .Where(p => p.EndsWith(".xml"))
                              .ToArray();
            try
            {
                using (StreamReader objfile = new StreamReader((assemblyFolder + "\\maps\\Objects.ini")))
                {
                    string line = objfile.ReadToEnd();
                    object_files = line.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't load a file Objects.ini!");
                Console.WriteLine(e.Message);
            }

            int filesx = 0;
            int alobj = 0;
            int alvehxs = 0;
            foreach (var path in files)
            {
                int objxs = 0;
                int vehxs = 0;
                Console.WriteLine("Map file load: " + files[filesx]);
                filesx++;
                XDocument xDoc = XDocument.Load(path);
                string map = xDoc.ToString();
                objxs = map.Split(new[] { "<Type>Prop</Type>" }, StringSplitOptions.None).Length - 1;
                vehxs = map.Split(new[] { "<Type>Vehicle</Type>" }, StringSplitOptions.None).Length - 1;
                Console.WriteLine("Loaded: Objects " + objxs + " / Vehicles: " + vehxs);
                alobj += objxs;
                alvehxs += vehxs;
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Loadeded " + filesx + " maps from " + assemblyFolder + "\\maps");
            Console.WriteLine("All loaded objects: " + alobj + " and " + alvehxs + " vehicles");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("MapLoader v1.0.1 by Crazzel http://time4games.pl/");
            Console.WriteLine();
            Console.ResetColor();
        }

        public override void Start()
        {
            LoadMapStart();
        }

        public override void OnPlayerConnect(User player)
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] files = Directory.GetFiles(assemblyFolder + "\\maps")
                              .Where(p => p.EndsWith(".xml"))
                              .ToArray();
            try
            {
                using (StreamReader objfile = new StreamReader((assemblyFolder + "\\maps\\Objects.ini")))
                {
                    string line = objfile.ReadToEnd();
                    object_files = line.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't load a file Objects.ini!");
                Console.WriteLine(e.Message);
            }

            int filesx = 0;
            foreach (var path in files)
            {
                filesx++;
                XDocument xDoc = XDocument.Load(path);
                string map = xDoc.ToString();
                string[] s = map.Split(new[] { "<MapObject>" }, StringSplitOptions.None);
                int objxs = map.Split(new[] { "<Type>Prop</Type>" }, StringSplitOptions.None).Length - 1;
                int vehxs = map.Split(new[] { "<Type>Vehicle</Type>" }, StringSplitOptions.None).Length - 1;
                string hash_name_c = "";
                VehicleHash hashx = VehicleHash.Infernus;
                for (int i = 1; i <= objxs + vehxs; i++)
                {
                    string typ = GetBetweenString("<Type>", "</Type>", s[i]);
                    bool place_on_ground = false;
                    if (typ == "Prop" || typ == "Vehicle")
                    {
                        bool load_not = true;
                        string hash = GetBetweenString("<Hash>", "</Hash>", s[i]);
                        string place = GetIfExist(s[i], "<Place>", "</Place>");

                        if (String.IsNullOrEmpty(place)) {
                            place_on_ground = false;
                        } else { place_on_ground = true; }

                        int strNumber; int strIndex = 0;
                        for (strNumber = 0; strNumber < object_files.Length; strNumber++)
                        {
                            strIndex = object_files[strNumber].IndexOf(hash);
                            if (strIndex >= 0)
                                break;
                            else
                                strIndex = -1;
                        }

                        if (strIndex == -1) {
                            hash_name_c = "";
                        } else {
                            string hash_name = object_files[strNumber];
                            hash_name_c = hash_name.Remove(hash_name.IndexOf("="));
                        }

                        if (String.IsNullOrEmpty(hash_name_c)) {
                            load_not = false;
                        }

                        if (typ == "Vehicle" && load_not == true) {
                            hash_name_c = FirstCharToUpper(hash_name_c);
                            if (hash_name_c == "Sabregt") { // Sabre have other Name
                                hash_name_c = "SabreGT";
                            }
                            hashx = (VehicleHash)System.Enum.Parse(typeof(VehicleHash), hash_name_c, true);
                        }

                        string position = GetBetweenString("<Position>", "</Position>", s[i]);
                        string pX = GetBetweenString("<X>", "</X>", position);
                        string pY = GetBetweenString("<Y>", "</Y>", position);
                        string pZ = GetBetweenString("<Z>", "</Z>", position);

                        string rotation = GetBetweenString("<Rotation>", "</Rotation>", s[i]);
                        string rX = GetBetweenString("<X>", "</X>", rotation);
                        string rY = GetBetweenString("<Y>", "</Y>", rotation);
                        string rZ = GetBetweenString("<Z>", "</Z>", rotation);

                        if (load_not == true)
                        {
                            if (typ == "Vehicle")
                            {
                                Vector3 pos = new Vector3();
                                pos.X = float.Parse(pX, CultureInfo.InvariantCulture);
                                pos.Y = float.Parse(pY, CultureInfo.InvariantCulture);
                                pos.Z = float.Parse(pZ, CultureInfo.InvariantCulture);
                                AddStaticVehicle(hashx, pos, float.Parse(rZ, CultureInfo.InvariantCulture));
                            }
                            if (typ == "Prop")
                            {
                                Vector3 pos = new Vector3();
                                pos.X = float.Parse(pX, CultureInfo.InvariantCulture);
                                pos.Y = float.Parse(pY, CultureInfo.InvariantCulture);
                                pos.Z = float.Parse(pZ, CultureInfo.InvariantCulture);
                                Vector3 rot = new Vector3();
                                rot.X = float.Parse(rX, CultureInfo.InvariantCulture);
                                rot.Y = float.Parse(rY, CultureInfo.InvariantCulture);
                                rot.Z = float.Parse(rZ, CultureInfo.InvariantCulture);
                                CreateObject(hash_name_c, pos, rot, place_on_ground);
                            }
                        }
                        else { Console.WriteLine("Couldn't load object / vehicle - invalid hash: [ " + hash + "]"); }
                    }
                }
            }
        }
    }
}
