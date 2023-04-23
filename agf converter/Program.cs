using System.Xml;
using System.Security.Cryptography;

string folder = "C:\\temp\\Fields";

List<string> agfFiles = Directory.GetFiles(folder, "*.agf").ToList();

foreach(string file in agfFiles)
{
    FileInfo fi = new FileInfo(file);
    string FileFirstName = fi.Name.Replace(".agf", "");
    string extractFolder = folder + "\\" + FileFirstName;
    if (!Directory.Exists(extractFolder))
    {
        Directory.CreateDirectory(extractFolder);

        System.IO.Compression.ZipFile.ExtractToDirectory(file, extractFolder);


        XmlDocument manifest = new XmlDocument();
        string manifestFile = extractFolder + "\\manifest.xml";
        manifest.Load(manifestFile);
        manifest.GetElementsByTagName("uuid");

        XmlNode? uuidXmlElement = manifest.GetElementsByTagName("uuid")[0];
        if (uuidXmlElement == null)
            throw (new Exception("Cant find UUID"));

        XmlNode? ivXmlElement = manifest.GetElementsByTagName("iv")[0];
        if (ivXmlElement == null)
            throw (new Exception("Cant find iv"));

        string uuid = uuidXmlElement.InnerText;
        string iv = ivXmlElement.InnerText;
        byte[] uuidhex = StringToByteArray(uuid.Replace("-", ""));
        byte[] numArray = StringToByteArray("e989715d4caa119b5fc8eac3ac46b7c3");

        byte[] key = exclusiveOR(uuidhex, numArray);


        Aes aes = Aes.Create();
        aes.BlockSize = 128;
        aes.Key = key;
        aes.IV = StringToByteArray(iv);
        ICryptoTransform decryptor = aes.CreateDecryptor();

        byte[] encFile = File.ReadAllBytes(extractFolder + "\\" + FileFirstName + ".xml.gz.enc");

        using (MemoryStream ms = new MemoryStream(encFile))
        {
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                using (StreamReader sr = new StreamReader(cs))
                {
                    File.WriteAllText(extractFolder + "\\" + FileFirstName + ".xml.gz", sr.ReadToEnd());
                }
            }
        }

    }
}




string encodedString = String.Empty;

XmlDocument doc = new XmlDocument();
try
{
    doc.Load("c:\\temp\\0f23d941-f5db-a43a-9c91-913781046f91.xml");
    XmlNode? boundaries = doc.GetElementsByTagName("field")[0];
    if(boundaries != null)
    {
        XmlNode? boundariesNode = boundaries["boundaries"];
        if(boundariesNode != null)
        {
            foreach(XmlNode boundary in boundariesNode.ChildNodes)
            {
                XmlNode? geometry = boundary["geometry"];
                if (geometry != null)
                    encodedString = geometry.InnerText;
            }
        }
    }
}
catch
{

}

byte[] data = Convert.FromBase64String(encodedString);
List<string> coordinates = new List<string>();

// 0 == Point, 1 == Line, 3 == Polygon, 4 == MultiPolygon

//Skip first 5 bytes
int i = 5;

while (i+24 <= data.Length)
{
    
    Double x = BitConverter.ToDouble(data, i + 0 * 8);
    Double y = BitConverter.ToDouble(data, i + 1 * 8);
    Double z = BitConverter.ToDouble(data, i + 2 * 8);


    CoordinateSharp.Coordinate loc = CoordinateSharp.ECEF.ECEFToLatLong(x / 1000, y / 1000, z / 1000);

    coordinates.Add(loc.Longitude.DecimalDegree + "," + loc.Latitude.DecimalDegree + ",0");

    i += 24;
}


using (XmlWriter writer = XmlWriter.Create("test2.kml", new XmlWriterSettings() { Indent = true, IndentChars = "  "}))
{
    writer.WriteStartDocument();
    writer.WriteStartElement("", "kml", "http://www.opengis.net/kml/2.2");
    writer.WriteStartElement("Placemark");
    writer.WriteElementString("name", "Hasseljordet Midt");
    writer.WriteStartElement("Polygon");
    writer.WriteStartElement("outerBoundaryIs");
    writer.WriteStartElement("LinearRing");
    writer.WriteElementString("coordinates", String.Join(" ", coordinates));
    writer.WriteEndElement();
    writer.WriteEndElement();
    writer.WriteEndElement();
    writer.WriteEndElement();
    writer.WriteEndElement();
    writer.WriteEndDocument();

}



static byte[] StringToByteArray(string hex)
{
    return Enumerable.Range(0, hex.Length)
                     .Select(x => Convert.ToByte(hex.Substring(x, 1), 16))
                     .ToArray();
}

static byte[] exclusiveOR(byte[] key, byte[] PAN)
{
    if (key.Length == PAN.Length)
    {
        byte[] result = new byte[key.Length];
        for (int i = 0; i < key.Length; i++)
        {
            result[i] = (byte)(key[i] ^ PAN[i]);
        }
        return result;
    }
    else
    {
        throw new ArgumentException();
    }
}