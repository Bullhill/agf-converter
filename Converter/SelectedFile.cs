using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Converter
{
    public class SelectedFile
    {
        public SelectedFile(string fileName)
        {
            FileName = fileName;
            FileFirstName = System.IO.Path.GetFileNameWithoutExtension(FileName);
        }

        public string FileName { get; set; } = String.Empty;
        public SelectedFile.StatusList Status { get 
            {
                return status; 
            } set { status = value; } }
        public string StatusText { get { return statusTextList[(int)status]; } }
        private StatusList status { get; set; } = StatusList.NoStatus;
        public string FileFirstName { get;}
        public List<CoordinateSharp.Coordinate> Coordinates { get; set; } = new List<CoordinateSharp.Coordinate>();
        public string FieldName { get; set; } = String.Empty ;
        public enum StatusList
        {
            NoStatus = 0,
            UnZipping = 1,
            UnZipped = 10,
            DeCrypting = 11,
            DeCryptFailed = 19,
            DeCrypted = 20,
            DecodeGeometry = 21,
            DecodedGeometry = 30,
            CreatingKML = 31,
            CreatedKML = 40,
            Complete = 100
        }
        private Dictionary<int, string> statusTextList = new Dictionary<int, string>()
        {
            {0, "-"},
            {1, "Unziping"},
            {10, "Unzipped" },
            {11, "Decrypting file" },
            {19, "Decrypting failed" },
            {20, "File decrypted" },
            {21, "Decoding geometry" },
            {30, "Geometry decoded" },
            {31, "Creating KML file" },
            {40, "KML file created" },
            {100, "Complete" }
        };

        public void DecodeGeometry(string xmlFilename)
        {
            Status = StatusList.DecodeGeometry;
            XmlDocument doc = new XmlDocument();

            string encodedString = String.Empty;
            try
            {
                doc.Load(xmlFilename);
                XmlNode? Field = doc.GetElementsByTagName("field")[0];
                if (Field != null)
                {
                    XmlNode? boundariesNode = Field["boundaries"];
                    if (boundariesNode != null)
                    {
                        foreach (XmlNode boundary in boundariesNode.ChildNodes)
                        {
                            XmlNode? geometry = boundary["geometry"];
                            if (geometry != null)
                                encodedString = geometry.InnerText;
                        }
                    }
                    XmlNode? fieldNameXml = Field["name"];
                    if (fieldNameXml != null)
                        FieldName = fieldNameXml.InnerText;

                }
            }
            catch
            {
                return;
            }

            byte[] data = Convert.FromBase64String(encodedString);

            // 0 == Point, 1 == Line, 3 == Polygon, 4 == MultiPolygon

            //Skip first 5 bytes
            int i = 5;

            while (i + 24 <= data.Length)
            {
                Double x = BitConverter.ToDouble(data, i + 0 * 8);
                Double y = BitConverter.ToDouble(data, i + 1 * 8);
                Double z = BitConverter.ToDouble(data, i + 2 * 8);

                Coordinates.Add(CoordinateSharp.ECEF.ECEFToLatLong(x / 1000, y / 1000, z / 1000));
                i += 24;
            }

        }
        public void CreateKML(string OutputFile)
        {
            Status = StatusList.CreatingKML;
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ",";
            using (XmlWriter writer = XmlWriter.Create(OutputFile, new XmlWriterSettings() { Indent = true, IndentChars = "  " }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("", "kml", "http://www.opengis.net/kml/2.2");
                writer.WriteStartElement("Placemark");
                writer.WriteElementString("name", FieldName);
                writer.WriteStartElement("Polygon");
                writer.WriteStartElement("outerBoundaryIs");
                writer.WriteStartElement("LinearRing");

                writer.WriteElementString("coordinates", String.Join(" ", Coordinates.Select(s => s.Longitude.DecimalDegree.ToString(nfi) + "," + s.Latitude.DecimalDegree.ToString(nfi))));
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();

            }
            Status = StatusList.CreatedKML;
        }
    }
}
