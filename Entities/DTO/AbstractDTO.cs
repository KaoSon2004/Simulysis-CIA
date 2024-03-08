using Entities.Logging;
using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Entities.DTO
{
    [Serializable]
    public abstract class AbstractDTO
    {
        #region Logging

        //private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly bool isDebugEnabled = true;

        #endregion
        public enum ActionType
        {
            [EnumMember]
            Null = 999
        }

        protected ActionType queryAction;

        #region Constructor

        /// <summary>
        /// Protected constructor to avoid instantiation
        /// </summary>
        protected AbstractDTO()
        {
        }

        #endregion

        /// <summary>
        /// Copies the current AbstractDTO to a new instance
        /// </summary>
        /// <returns>A shallowed copy if the current object</returns>
        public AbstractDTO Copy()
        {
            return MemberwiseClone() as AbstractDTO;
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            Type t = GetType();
            FieldInfo[] fields =
                t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo info in fields)
            {
                res.AppendFormat("{0}: [{1}], ", info.Name, info.GetValue(this));
            }

            return res.ToString();
        }


        #region XmlSerialization

        /// <summary>
        /// Læser et IDictionary type object fra xml og instansierer det. (Læser det format som er output fra WriteXmlDictionary)
        /// </summary>
        /// <param name="r">XmlReader</param>
        /// <param name="typeName">Det fulde type navn på den klasse som implemeterer IDictionary feks. System.Collections.SortedList</param>
        /// <param name="name">string identifier</param>
        /// <returns>Det instansierede object fra xml som implementerer IDictionary</returns>
        private IDictionary ReadXmlDictionary(XmlReader r,
            string typeName, string name)
        {
            if (isDebugEnabled)
            {

            }

            IDictionary dictionary;

            dictionary =
                (IDictionary)Type.GetType(typeName).GetConstructor(
                new Type[0] { }).Invoke(new object[0] { });

            r.MoveToContent();

            while (r.NodeType != XmlNodeType.EndElement)
            {
                ReadStartElement(r, "item");

                object key = ReadXml(r, "key");
                object item = ReadXml(r, "value");

                if (!dictionary.Contains(key))
                {
                    dictionary.Add(key, item);
                }

                ReadEndElement(r);
            }

            return dictionary;
        }

        /// <summary>
        /// Skriver et object af IDictionary til xml. (Skriver det format som er input til ReadXmlDictionary)
        /// </summary>
        /// <param name="w">XmlWriter</param>
        /// <param name="dictionary">Instansen af det IDictionary som skal skrives som xml</param>
        /// <param name="name">string identifier</param>
        private void WriteXmlDictionary(XmlWriter w, IDictionary dictionary, string name)
        {
            foreach (object key in dictionary.Keys)
            {
                w.WriteStartElement("item", "");

                WriteXml(w, key, "key");

                WriteXml(w, dictionary[key], "value");

                w.WriteEndElement();
            }
        }

        /// <summary>
        /// Skriver et object af IEnumerable til xml. (Skriver det format som er input til ReadXmlIEnumerableIList)
        /// </summary>
        /// <param name="w">XmlWriter</param>
        /// <param name="array">Instansen af det IEnumerable som skal skrives som xml</param>
        /// <param name="name">string identifier</param>
        private void WriteXmlIEnumerableIList(XmlWriter w, IEnumerable array, string name)
        {
            foreach (object arrayValue in array)
            {
                WriteXml(w, arrayValue, "value");
            }
        }

        /// <summary>
        /// Læser et IList type object fra xml og instansierer det. (Læser det format som er output fra WriteXmlIEnumerableIList) 
        /// </summary>
        /// <param name="r">XmlReader</param>
        /// <param name="typeName">Det fulde type navn på den klasse som implemetere ArrayList feks. System.Collections.ArrayList</param>
        /// <param name="name">string identifier</param>
        /// <returns>Det instansierede object fra xml som implementerer IList</returns>
        private IList ReadXmlIEnumerableIList(XmlReader r, string typeName, string name)
        {
            IList arrayList = (IList)Type.GetType(typeName).GetConstructor(new Type[0] { }).Invoke(new object[0] { });

            while (r.NodeType != XmlNodeType.EndElement)
            {
                object arrayValue = ReadXml(r, "value");

                arrayList.Add(arrayValue);
            }

            return arrayList;
        }

        protected void WriteSchema(ref XmlSchemaSequence xmlSchemaSequence, object item, string name)
        {
            try
            {
                XmlQualifiedName xmlQualifiedName;
                XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
                xmlSchemaElement.Name = name;

                if (null != item)
                {
                    if (item.GetType() == typeof(ActionType))
                    {
                        xmlQualifiedName = new XmlQualifiedName(
                            "string",
                            "http://www.w3.org/2001/XMLSchema"
                            );
                        xmlSchemaElement.SchemaTypeName = xmlQualifiedName;
                        xmlSchemaSequence.Items.Add(xmlSchemaElement);
                    }
                    else if (item.GetType() == typeof(DateTime))
                    {

                    }
                    else if (item.GetType() == typeof(bool))
                    {
                        xmlQualifiedName = new XmlQualifiedName(
                            "boolean",
                            "http://www.w3.org/2001/XMLSchema"
                            );
                        xmlSchemaElement.SchemaTypeName = xmlQualifiedName;
                        xmlSchemaSequence.Items.Add(xmlSchemaElement);
                    }
                    else if (item.GetType() == typeof(long))
                    {
                        xmlQualifiedName = new XmlQualifiedName(
                            "long",
                            "http://www.w3.org/2001/XMLSchema"
                            );
                        xmlSchemaElement.SchemaTypeName = xmlQualifiedName;
                        xmlSchemaSequence.Items.Add(xmlSchemaElement);
                    }
                    else if (item.GetType() == typeof(int))
                    {
                        xmlQualifiedName = new XmlQualifiedName(
                            "integer",
                            "http://www.w3.org/2001/XMLSchema"
                            );
                        xmlSchemaElement.SchemaTypeName = xmlQualifiedName;
                        xmlSchemaSequence.Items.Add(xmlSchemaElement);
                    }
                    else if (item.GetType() == typeof(string))
                    {
                        xmlQualifiedName = new XmlQualifiedName(
                            "string",
                            "http://www.w3.org/2001/XMLSchema"
                            );
                        xmlSchemaElement.SchemaTypeName = xmlQualifiedName;
                        xmlSchemaSequence.Items.Add(xmlSchemaElement);
                    }

                }
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                //Logging.Loggers.DigitalSignManager.Exception(exception);
            }
        }

        protected void WriteSchemaNameSpace(ref XmlSchema xmlSchema, string nameSpace)
        {
            xmlSchema.Namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
            xmlSchema.Namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xmlSchema.Namespaces.Add("tns",  nameSpace);
            xmlSchema.Id = nameSpace + "Schema";
            xmlSchema.TargetNamespace = nameSpace;

        }

        /// <summary>
        /// Skriver et object til xml. (Skriver det format som er input til ReadXml) OBS. Ikke alle typer objecter kan XmlSerializeres DateTime, ArrayList, IDictionary og simple typer er understøttet atm.
        /// </summary>
        /// <param name="w">XmlWriter</param>
        /// <param name="item">Instansen af det object der skal skrives til xml</param>
        /// <param name="name">string identifier</param>
        protected void WriteXml(XmlWriter w, object item, string name)
        {
            w.WriteStartElement(name, "");

            if (item == null)
            {
                w.WriteStartElement("type", "");
                w.WriteString("NULL");
                w.WriteEndElement();
            }
            else
            {
                w.WriteStartElement("type", "");
                w.WriteString(item.GetType().ToString());
                w.WriteEndElement();

                if (item.GetType() == typeof(DateTime))
                {
                    w.WriteStartElement("DateTime", "");
                    w.WriteString(((DateTime)item).ToString("G", new CultureInfo("en-GB")));
                    w.WriteEndElement();
                }
                else if (item.GetType() == typeof(Bitmap))
                {
                    w.WriteStartElement("Bitmap", "");

                    MemoryStream ms = new MemoryStream();
                    BinaryFormatter bf1 = new BinaryFormatter();
                    bf1.Serialize(ms, item);

                    w.WriteString(Convert.ToBase64String(ms.ToArray()));
                    w.WriteEndElement();
                }
                else if (item.GetType().GetInterface("IDictionary") != null)
                {
                    WriteXmlDictionary(w, (IDictionary)item, name);
                }
                else if (item.GetType().GetInterface("IEnumerable") != null && item.GetType().GetInterface("IList") != null)
                {
                    WriteXmlIEnumerableIList(w, (IEnumerable)item, name);
                }
                else
                {
                    XmlSerializer pageTitleSerialize = new XmlSerializer(item.GetType());
                    pageTitleSerialize.Serialize(w, item);
                }
            }
            w.WriteEndElement();
        }

        /// <summary>
        /// Læser et object fra xml og instansierer det. (Læser det format som er output fra WriteXml) OBS. Ikke alle typer objecter kan XmlSerializeres DateTime, ArrayList, IDictionary og simple typer er understøttet atm.
        /// </summary>
        /// <param name="r">XmlReader</param>
        /// <param name="name">string identifier</param>
        /// <returns>Det instansierede object fra xml</returns>
        protected object ReadXml(XmlReader r, string name)
        {
            string typeName;
            object item;
            XmlSerializer pageTitleSerialize;

            ReadStartElement(r, name);
            ReadStartElement(r, "type");
            typeName = ReadString(r);

            ReadEndElement(r);

            if (typeName == "NULL")
            {
                item = null;
            }
            else
            {
                if (typeName == typeof(DateTime).ToString())
                {
                    ReadStartElement(r, "DateTime");
                    string dateTime = ReadString(r);
                    item = Convert.ToDateTime(dateTime, new CultureInfo("en-GB"));
                    ReadEndElement(r);
                }
                else if (typeName == typeof(Bitmap).ToString())
                {
                    ReadStartElement(r, "Bitmap");
                    MemoryStream ms = new MemoryStream(Convert.FromBase64String(ReadString(r)));
                    BinaryFormatter bf1 = new BinaryFormatter();
                    ms.Position = 0;

                    item = bf1.Deserialize(ms);
                    ReadEndElement(r);
                }
                else if (Type.GetType(typeName).GetInterface("IDictionary") != null)
                {
                    item = ReadXmlDictionary(r, typeName, name);
                }
                else if (Type.GetType(typeName).GetInterface("IList") != null && Type.GetType(typeName).GetInterface("IEnumerable") != null)
                {
                    item = ReadXmlIEnumerableIList(r, typeName, name);
                }
                else
                {
                    pageTitleSerialize = new XmlSerializer(Type.GetType(typeName));
                    item = pageTitleSerialize.Deserialize(r);
                }
            }
            ReadEndElement(r);

            return item;
        }

        /// <summary>
        /// Checker at det næste content element er et start element med den angivne identifier. Følgende typer nodes over: ProcessingInstruction, DocumentType, Comment, WhiteSpace, SignificantWhiteSpace.
        /// </summary>
        /// <param name="r">XmlReader</param>
        /// <param name="name">string identifier</param>
        private void ReadStartElement(XmlReader r, string name)
        {
            r.MoveToContent();
            r.ReadStartElement(name);
        }

        /// <summary>
        /// Checker at det næste content element er et end element med den angivne identifier. Følgende typer nodes over: ProcessingInstruction, DocumentType, Comment, WhiteSpace, SignificantWhiteSpace.
        /// </summary>
        /// <param name="r">XmlReader</param>
        private void ReadEndElement(XmlReader r)
        {
            r.MoveToContent();
            r.ReadEndElement();
        }

        /// <summary>
        /// Læser content af næste element eller textnode som string. Følgende typer nodes over: ProcessingInstruction, DocumentType, Comment, WhiteSpace, SignificantWhiteSpace.
        /// </summary>
        /// <param name="r">XmlReader</param>
        /// <returns>Den læste string</returns>
        private string ReadString(XmlReader r)
        {
            string result;
            r.MoveToContent();
            result = r.ReadString();

            return result;
        }

        /// <summary>
        /// Add this property to recognize type of DTO.
        /// </summary>
    }

    #endregion
}
