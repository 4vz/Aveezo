using AngleSharp.Dom;
using AngleSharp.Xml.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Aveezo
{
    public abstract class XmlObjectBase
    {
        public string Prefix { get; set; }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public string Value { get; set; }
    }

    public class XmlObject : XmlObjectBase
    {
        #region Fields

        public Type Type { get; internal set; } = null;

        public List<XmlObjectElementAttribute> Attributes { get; } = new List<XmlObjectElementAttribute>();

        public XmlObjectAttributeOptions Options { get; set; } = XmlObjectAttributeOptions.None;

        public XmlObjectSchemaName SchemaName { get; set; } = XmlObjectSchemaName.None;

        public XmlObjectSchemaElement SchemaGroup { get; set; } = XmlObjectSchemaElement.None;

        #endregion
    }

    public sealed class XmlObjectElement : XmlObject
    {
        #region Fields

        public XmlObjectElement Parent { get; internal set; } = null;

        internal Dictionary<string, XmlObjectElement> Elements { get; } = new Dictionary<string, XmlObjectElement>();

        public bool IsSerialized => typeCollections.SerializableTypes.Contains(Type);

        private TypeRepository typeCollections;

        public PropertyInfo Property { get; internal set; } = null;

        internal XmlPrefix XmlPrefix { get; }

        #endregion

        #region Constructors

        internal XmlObjectElement(XmlPrefix xmlPrefix, TypeRepository typeCollections)
        {
            XmlPrefix = xmlPrefix;
            this.typeCollections = typeCollections;
        }

        #endregion

        #region Methods

        public bool IsDescendantOf(XmlObjectElement element)
        {
            if (element == null)
                return false;
            else if (this == element)
                return true;
            else if (Parent == null)
                return false;
            else
                return Parent.IsDescendantOf(element);
        }

        public bool IsAncestorHas(XmlObjectAttributeOptions options)
        {
            if (Parent == null)
                return false;
            else if (Parent.Options.HasFlag(options))
                return true;
            else
                return Parent.IsAncestorHas(options);
        }

        public XmlObjectElement Add(string name) => Add(name, null, null, null);

        public XmlObjectElement Add(string name, object obj) => Add(name, obj, null, null);

        public XmlObjectElement Add(object obj, EventHandler<XmlObjectEventArgs> handler) => Add(null, obj, null, handler);

        public XmlObjectElement Add(EventHandler<XmlObjectEventArgs> handler) => Add(null, null, null, handler);

        public XmlObjectElement Add(string name, EventHandler<XmlObjectEventArgs> handler) => Add(name, null, null, handler);

        public XmlObjectElement Add(string name, object obj, EventHandler<XmlObjectEventArgs> handler) => Add(name, obj, null, handler);

        public XmlObjectElement Add(object obj, XmlObject overrideObject) => Add(null, obj, overrideObject, null);

        public XmlObjectElement Add(XmlObject overrideObject) => Add(null, null, overrideObject, null);

        public XmlObjectElement Add(string name, XmlObject overrideObject) => Add(name, null, overrideObject, null);

        public XmlObjectElement Add(string name, object obj, XmlObject overrideObject) => Add(name, obj, overrideObject, null);

        public XmlObjectElement Add(object obj, XmlObject overrideObject, EventHandler<XmlObjectEventArgs> handler) => Add(null, obj, overrideObject, handler);

        public XmlObjectElement Add(XmlObject overrideObject, EventHandler<XmlObjectEventArgs> handler) => Add(null, null, overrideObject, handler);

        public XmlObjectElement Add(string name, XmlObject overrideObject, EventHandler<XmlObjectEventArgs> handler) => Add(name, null, overrideObject, handler);

        public XmlObjectElement Add(string name, object obj, XmlObject overrideObject, EventHandler<XmlObjectEventArgs> handler)
        {
            if (name == null)
                name = Rnd.String(10, Collections.WordDigitUnderscore);

            if (!Elements.ContainsKey(name))
            {
                if (obj == null)
                    obj = new object();

                var build = Xml.Build(obj, XmlPrefix, typeCollections, handler, null, this, overrideObject);
                Elements.Add(name, build);
                return build;
            }
            else
                return Elements[name];
        }

        public XmlObjectElement Add(XmlObjectSchemaElement schemaElement)
        {
            var element = Add($"_xml_{schemaElement}", new XmlObject { Options = XmlObjectAttributeOptions.XmlSchema });

            element.SchemaName = schemaElement switch
            {
                XmlObjectSchemaElement.Sequence => XmlObjectSchemaName.Sequence,
                _ => XmlObjectSchemaName.None
            };

            return element;
        }

        #endregion
    }

    public sealed class XmlObjectElementAttribute : XmlObjectBase
    {
        #region Constructors

        public XmlObjectElementAttribute(string prefix, string name, string ns, string value)
        {
            Prefix = prefix;
            Name = name;
            Namespace = ns;
            Value = value;
        }

        public XmlObjectElementAttribute(string name, string value) : this(null, name, null, value)
        {
        }

        #endregion

        #region Statics

        public static XmlObjectElementAttribute XmlnsPrefix(string name, string value) => new XmlObjectElementAttribute("xmlns", name, null, value);

        public static XmlObjectElementAttribute NameAttribute(string value) => new XmlObjectElementAttribute(null, "name", null, value);

        public static XmlObjectElementAttribute TypeAttribute(string value) => new XmlObjectElementAttribute(null, "type", null, value);

        #endregion
    }

    public interface IXmlObject
    {
        public XmlObject Object { get; }
    }

    public enum XmlObjectSchemaElement
    {
        None,
        Sequence
    }

    public enum XmlObjectSchemaName
    {
        None,
        ComplexType,
        Element,
        Attribute,
        Sequence,
        Item
    }

    [Flags]
    public enum XmlObjectAttributeOptions
    {
        None = 0,
        HideTypeDeclaration = 1,
        HideValue = 2,
        XmlSchema = 4,
        NameDeclaration = 8,
        XmlSchemaElement = XmlSchema | HideValue | NameDeclaration
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class XmlObjectAttribute : Attribute
    {
        public XmlObjectAttributeOptions Options { get; } = XmlObjectAttributeOptions.None;

        public XmlObjectAttribute(XmlObjectAttributeOptions options)
        {
            Options = options;
        }
    }

    public sealed class XmlObjectEventArgs : EventArgs
    {
        public XmlObjectElement Element { get; init; }

        public XmlObjectElement Parent { get; init; }

        public bool IsRoot => Parent == null;
    }

    public sealed class XmlElementEventArgs : EventArgs
    {
        public IElement Element { get; init; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class XmlHiddenAttribute : Attribute { }

    public static class Xml
    {
        #region Statics

        public static async Task<string> Serialize(object obj, XmlPrefix xmlPrefix, TypeRepository typeCollections, EventHandler<XmlObjectEventArgs> handler)
        {
            using var stringWriter = new CustomEncodingStringWriter(Encoding.UTF8);
            await Serialize(stringWriter, obj, xmlPrefix, typeCollections, handler);
            return stringWriter.ToString();
        }

        public static async Task Serialize(TextWriter textWriter, object obj, XmlPrefix xmlPrefix, TypeRepository typeCollections, EventHandler<XmlObjectEventArgs> handler)
        {
            using var xmlWriter = XmlWriter.Create(textWriter, new XmlWriterSettings { Async = true, Encoding = Encoding.UTF8 });
            await Serialize(xmlWriter, obj, xmlPrefix, typeCollections, handler);
        }

        public static async Task Serialize(XmlWriter xmlWriter, object obj, XmlPrefix xmlPrefix, TypeRepository typeCollections, EventHandler<XmlObjectEventArgs> handler)
        {
            var root = Build(obj, xmlPrefix, typeCollections, handler, null, null, null);
            Validate(root, xmlPrefix);
            await Write(xmlWriter, root);            
            await xmlWriter.FlushAsync();
        }

        public static string GetType(XmlPrefix xmlPrefix, Type type)
        {
            if (type == null)
                return null;

            var name = type.Name switch
            {
                "Boolean" => $"{xmlPrefix.XmlDefinition}:boolean",
                "SByte" => $"{xmlPrefix.XmlDefinition}:byte",
                "Byte" => $"{xmlPrefix.XmlDefinition}:unsignedByte",
                "Int16" => $"{xmlPrefix.XmlDefinition}:short",
                "UInt16" => $"{xmlPrefix.XmlDefinition}:unsignedShort",
                "Int32" => $"{xmlPrefix.XmlDefinition}:int",
                "UInt32" => $"{xmlPrefix.XmlDefinition}:unsignedInt",
                "Int64" => $"{xmlPrefix.XmlDefinition}:long",
                "UInt64" => $"{xmlPrefix.XmlDefinition}:unsignedLong",
                "Char" => $"{xmlPrefix.Local}:char",
                "Single" => $"{xmlPrefix.XmlDefinition}:float",
                "Double" => $"{xmlPrefix.XmlDefinition}:double",
                "Decimal" => $"{xmlPrefix.XmlDefinition}:decimal",
                "DateTime" => $"{xmlPrefix.XmlDefinition}:dateTime",
                "DateTimeOffset" => $"{xmlPrefix.XmlDefinition}:dateTime",
                "TimeSpan" => $"{xmlPrefix.XmlDefinition}:duration",
                "String" => $"{xmlPrefix.XmlDefinition}:string",
                "Guid" => $"{xmlPrefix.Local}:uuid",
                "BitArray" => $"{xmlPrefix.Local}:bitarray",
                "PhysicalAddress" => $"{xmlPrefix.Local}:mac",
                "IPAddressCidr" => $"{xmlPrefix.Local}:cidr",
                "IPAddress" => $"{xmlPrefix.Local}:ip",
                "Object" => $"{xmlPrefix.Local}:object",
                _ => null
            };

            if (name == null)
            {
                if (type.IsArray)
                    name = $"{xmlPrefix.Local}:array";
                else if (type.IsGenericType)
                {
                    var genericType = type.GetGenericTypeDefinition();

                    if (genericType == typeof(List<>))
                        name = $"{xmlPrefix.Local}:list";
                    else if (genericType == typeof(Dictionary<,>))
                        name = $"{xmlPrefix.Local}:dictionary";
                }
            }

            if (name == null)
            {
            //    name = $"{xmlPrefix.Local}:object";
            }

            return name;
        }

        private static string GetString(object obj) => obj switch
        {
            bool o => o ? "true" : "false",
            sbyte o => o.ToString().ToAscii(),
            byte o => o.ToString().ToAscii(),
            short o => XmlConvert.ToString(o),
            ushort o => XmlConvert.ToString(o),
            int o => XmlConvert.ToString(o),
            uint o => XmlConvert.ToString(o),
            long o => XmlConvert.ToString(o),
            ulong o => XmlConvert.ToString(o),
            char o => o.ToString().ToAscii(),
            float o => XmlConvert.ToString(o),
            double o => XmlConvert.ToString(o),
            decimal o => XmlConvert.ToString(o),
            DateTime o => XmlConvert.ToString(o, XmlDateTimeSerializationMode.Utc),
            DateTimeOffset o => XmlConvert.ToString(o),
            TimeSpan o => o.ToISO8601(),
            string o => o.ToString().ToAscii(),
            Guid o => XmlConvert.ToString(o),
            BitArray o => o.ToString('0', '1'),
            PhysicalAddress o => o.ToString(":"),
            IPAddressCidr o => o.ToString(),
            IPAddress o => o.ToString(),
            _ => obj.ToString()
        };

        private static object GetObject(string value, Type type)
        {
            if (string.IsNullOrEmpty(value)) return null;

            if (type == typeof(bool)) return XmlConvert.ToBoolean(value);
            else if (type == typeof(sbyte)) return XmlConvert.ToSByte(value);
            else if (type == typeof(byte)) return XmlConvert.ToByte(value);
            else if (type == typeof(short)) return XmlConvert.ToInt16(value);
            else if (type == typeof(ushort)) return XmlConvert.ToUInt16(value);
            else if (type == typeof(int)) return XmlConvert.ToInt32(value);
            else if (type == typeof(uint)) return XmlConvert.ToUInt32(value);
            else if (type == typeof(long)) return XmlConvert.ToInt64(value);
            else if (type == typeof(ulong)) return XmlConvert.ToUInt64(value);
            else if (type == typeof(char)) return XmlConvert.ToChar(value);
            else if (type == typeof(float)) return XmlConvert.ToSingle(value);
            else if (type == typeof(double)) return XmlConvert.ToDouble(value);
            else if (type == typeof(decimal)) return XmlConvert.ToDecimal(value);
            else if (type == typeof(DateTime)) return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Utc);
            else if (type == typeof(DateTimeOffset)) return XmlConvert.ToDateTimeOffset(value);
            else if (type == typeof(TimeSpan)) return TimeSpanUtil.Parse(value);
            else if (type == typeof(string)) return value;
            else if (type == typeof(Guid)) return XmlConvert.ToGuid(value);
            else if (type == typeof(BitArray)) return value.ToBitArray();
            else if (type == typeof(PhysicalAddress)) return NetworkEquipment.ParsePhysicalAddress(value);
            else if (type == typeof(IPAddressCidr)) return IPAddressCidr.Parse(value);
            else if (type == typeof(IPAddress)) return IPAddressCidr.Parse(value)?.IPAddress;
            else return default;
        }

        private static void AddTypeDeclaration(XmlObjectElement element)
        {
            var type = GetType(element.XmlPrefix, element.Type);

            if (type != null)
            {
                element.Attributes.Add(new XmlObjectElementAttribute(element.XmlPrefix.XmlInstance, "type", null, type));

                if (type == $"{element.XmlPrefix.Local}:array")
                {
                    var subType = GetType(element.XmlPrefix, element.Type.GetElementType());
                    if (subType != null) element.Attributes.Add(new XmlObjectElementAttribute(element.XmlPrefix.Local, "arrayType", null, subType));
                }
                else if (type == $"{element.XmlPrefix.Local}:list")
                {
                    var subType = GetType(element.XmlPrefix, element.Type.GetGenericArguments()[0]);
                    if (subType != null) element.Attributes.Add(new XmlObjectElementAttribute(element.XmlPrefix.Local, "listType", null, subType));
                }
                else if (type == $"{element.XmlPrefix.Local}:dictionary")
                {
                    var types = element.Type.GetGenericArguments();

                    var keyType = GetType(element.XmlPrefix, types[0]);
                    var valueType = GetType(element.XmlPrefix, types[1]);

                    if (keyType != null) element.Attributes.Add(new XmlObjectElementAttribute(element.XmlPrefix.Local, "dictionaryKeyType", null, keyType));
                    if (valueType != null) element.Attributes.Add(new XmlObjectElementAttribute(element.XmlPrefix.Local, "dictionaryValueType", null, valueType));
                }
            }
        }

        public static void AddBuiltInSchemaElements(XmlObjectElement element)
        {
            element.Add(new XmlObject { Name = "array", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Element }, (obj, args) =>
            {
                args.Element.Add(new XmlObject { Name = "arrayType", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Attribute });
            });
            element.Add(new XmlObject { Name = "list", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Element }, (obj, args) =>
            {
                args.Element.Add(new XmlObject { Name = "listType", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Attribute });
            });
            element.Add(new XmlObject { Name = "dictionary", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Element }, (obj, args) =>
            {
                args.Element.Add(new XmlObject { Name = "dictionaryKeyType", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Attribute });
                args.Element.Add(new XmlObject { Name = "dictionaryValueType", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Attribute });

            });
            element.Add(new XmlObject { Name = "char", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Element });
            element.Add(new XmlObject { Name = "uuid", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Element });
            element.Add(new XmlObject { Name = "key", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Element });
            element.Add(new XmlObject { Name = "item", Options = XmlObjectAttributeOptions.XmlSchemaElement, SchemaName = XmlObjectSchemaName.Element });

        }

        public static void AddTargetNamespace(XmlObject obj, string targetNamespace)
        {
            obj.Attributes.Add(new XmlObjectElementAttribute(null, "targetNamespace", null, targetNamespace));
        }

        public static void AddSchemaNamespace(XmlObject obj, string schemaNamespace, string operationNamespace)
        {
            obj.Attributes.Add(XmlObjectElementAttribute.XmlnsPrefix("m", operationNamespace));
            obj.Attributes.Add(XmlObjectElementAttribute.XmlnsPrefix("a", schemaNamespace));
        }

        private static void Validate(XmlObjectElement root, XmlPrefix xmlPrefix)
        {
            SetPrefixNamespace(Search(root, xmlPrefix.Local), xmlPrefix.Local, xmlPrefix.LocalNamespace);
            SetPrefixNamespace(Search(root, xmlPrefix.XmlDefinition), xmlPrefix.XmlDefinition, xmlPrefix.XmlDefinitionNamespace);
            SetPrefixNamespace(Search(root, xmlPrefix.XmlInstance), xmlPrefix.XmlInstance, xmlPrefix.XmlInstanceNamespace);

            foreach (var nsp in xmlPrefix.Namespaces)
            {
                SetPrefixNamespace(Search(root, nsp.Key), nsp.Key, nsp.Value);
            }
        }
        
        private static void SetPrefixNamespace(XmlObjectElement element, string prefix, string ns)
        {
            if (element != null)
            {
                if (element.Prefix == prefix)
                    element.Namespace = ns;
                else
                    element.Attributes.Add(new XmlObjectElementAttribute("xmlns", prefix, null, ns));
            }
        }

        private static XmlObjectElement Search(XmlObjectElement element, string prefix)
        {
            if (element.Prefix == prefix)
                return element;

            foreach (var attribute in element.Attributes)
            {
                if (attribute.Prefix == prefix)
                    return element;
                else if (attribute.Value != null && attribute.Value.StartsWith($"{prefix}:"))
                    return element;
            }

            XmlObjectElement mcra = null;
            var numofchild = 0;

            foreach (var childElement in element.Elements)
            {
                var ce = Search(childElement.Value, prefix);

                if (ce != null)
                {
                    mcra = ce;
                    numofchild++;
                }
                if (numofchild > 1)
                {
                    mcra = element;
                    break;
                }
            }

            return mcra;
        }

        internal static XmlObjectElement Build(object obj, XmlPrefix xmlPrefix, TypeRepository typeCollections, EventHandler<XmlObjectEventArgs> handler, PropertyInfo objectProperty, XmlObjectElement parent, XmlObject overrideObject)
        {
            if (obj == null && objectProperty == null) return null;

            var type = obj != null ? obj.GetType() : objectProperty.PropertyType;
            var element = new XmlObjectElement(xmlPrefix, typeCollections) { Type = type, Parent = parent, Property = objectProperty };

            XmlObject objectInterface = null;

            // get element configuration from XmlObject
            if (obj is IXmlObject xobj && xobj.Object != null)
            {
                objectInterface = xobj.Object;

                if (objectInterface.Name != null)
                    element.Name = objectInterface.Name;

                element.Prefix = objectInterface.Prefix;
                element.Namespace = objectInterface.Namespace;
                element.Attributes.AddRange(objectInterface.Attributes);

                element.Value = objectInterface.Value;
                element.Options |= objectInterface.Options;
                element.SchemaName = objectInterface.SchemaName;
                element.SchemaGroup = objectInterface.SchemaGroup;
            }
            if (overrideObject != null)
            {
                if (overrideObject.Name != null)
                    element.Name = overrideObject.Name;
                if (overrideObject.Prefix != null)
                    element.Prefix = overrideObject.Prefix;
                if (overrideObject.Namespace != null)
                    element.Namespace = overrideObject.Namespace;
                element.Attributes.AddRange(overrideObject.Attributes);

                element.Value = overrideObject.Value;
                element.Options |= overrideObject.Options;
                element.SchemaName = overrideObject.SchemaName;
                element.SchemaGroup = overrideObject.SchemaGroup;
            }

            // get element configuration from objectProperty
            if (objectProperty != null && objectProperty.Has<XmlObjectAttribute>(out var xobja))
                element.Options |= xobja[0].Options;
                        
            // handler process
            handler?.Invoke(obj, new XmlObjectEventArgs { Element = element, Parent = parent });

            // if name null, using property name or use type name
            if (element.Name == null)
            {
                if (objectProperty == null)
                    element.Name = element.Type.Name.Keep("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_");
                else
                    element.Name = objectProperty.Name;
            }

            var options = element.Options;

            // apply options before child iteration
            if (!options.HasFlag(XmlObjectAttributeOptions.HideTypeDeclaration))
                AddTypeDeclaration(element);
            if (options.HasFlag(XmlObjectAttributeOptions.XmlSchema))
            {
                if (element.Prefix == null) element.Prefix = xmlPrefix.XmlDefinition;

                //if (!element.IsAncestorHas(XmlObjectAttributeOptions.XmlSchema))
                //{
                //    if (element.Namespace == null)
                //        element.Namespace = "http://www.w3.org/2001/XMLSchema";
                //    else
                //        element.Attributes.Add(new XmlObjectElementAttribute("xmlns", xmlPrefix.XmlDefinition, null, "http://www.w3.org/2001/XMLSchema"));
                //    element.Attributes.Add(new XmlObjectElementAttribute("xmlns", xmlPrefix.XmlInstance, null, "http://www.w3.org/2001/XMLSchema-instance"));
                //}
            }
            if (options.HasFlag(XmlObjectAttributeOptions.NameDeclaration))
                element.Attributes.Add(new XmlObjectElementAttribute(xmlPrefix.XmlInstance, "name", null, $"{element.Name}"));
            if (!element.IsSerialized && !options.HasFlag(XmlObjectAttributeOptions.HideValue) && element.Value == null && obj != null)
            {
                if (obj is Array || obj is IList)
                {
                    foreach (var item in (IList)obj)
                        element.Add(item, new XmlObject { Prefix = xmlPrefix.Local, Name = "Item", Options = XmlObjectAttributeOptions.HideTypeDeclaration });
                }
                else if (obj is IDictionary dictionary)
                {
                    foreach (DictionaryEntry item in dictionary)
                    {
                        element.Add(item.Value, new XmlObject { Prefix = xmlPrefix.Local, Name = "Item", Options = XmlObjectAttributeOptions.HideTypeDeclaration }, (obj, args) =>
                        {
                            args.Element.Attributes.Add(new XmlObjectElementAttribute(xmlPrefix.Local, "key", null, item.Key.ToString()));
                        });

                    }
                }
                else
                    element.Value = GetString(obj);
            }
            
            // iterate to children
            if (element.IsSerialized)
            {
                foreach (var property in type.GetProperties())
                {
                    if ((objectInterface != null && property.Name == "Object") ||
                        property.Has<XmlHiddenAttribute>()
                        ) continue; // skip the IXmlObject's Object and Hidden

                    var childElement = Build(property.GetValue(obj), xmlPrefix, typeCollections, handler, property, element, null);

                    var becomeSchemaGroup = false;

                    if (childElement.SchemaGroup != XmlObjectSchemaElement.None)
                    {
                        var schemaGroup = $"_xml_{childElement.SchemaGroup}";

                        if (element.Elements.ContainsKey(schemaGroup))
                        {
                            var newParent = element.Elements[schemaGroup];
                            childElement.Parent = newParent;
                            newParent.Elements.Add(property.Name, childElement);
                            becomeSchemaGroup = true;
                        }
                    }

                    if (!becomeSchemaGroup)
                        element.Elements.Add(property.Name, childElement);
                }
            }

            return element;
        }

        private static async Task Write(XmlWriter xmlWriter, XmlObjectElement element)
        {
            string elementName = element.SchemaName switch
            {
                XmlObjectSchemaName.ComplexType => "complexType",
                XmlObjectSchemaName.Element => "element",
                XmlObjectSchemaName.Attribute => "attribute",
                XmlObjectSchemaName.Sequence => "sequence",
                XmlObjectSchemaName.Item => "item",
                _ => element.Name.CleanSpaces()
            };

            await xmlWriter.WriteStartElementAsync(element.Prefix, elementName, element.Namespace);

            foreach (var attribute in element.Attributes)
                await xmlWriter.WriteAttributeStringAsync(attribute.Prefix, attribute.Name, attribute.Namespace, attribute.Value);

            if (element.Elements.Count == 0 && element.Value != null)
                await xmlWriter.WriteStringAsync(element.Value);

            foreach (var (_, childElement) in element.Elements)
                await Write(xmlWriter, childElement);

            await xmlWriter.WriteEndElementAsync();
        }

        public static T Deserialize<T>(string xml, TypeRepository typeCollections) where T : IXmlObject
        {
            var parser = new XmlParser();
            var xdoc = parser.ParseDocument(xml);

            T obj = (T)Build(xdoc.DocumentElement, typeof(T), typeCollections);

            return obj;
        }

        private static object Build(IElement element, Type type, TypeRepository typeCollections)
        {
            object obj = null;
            object o = null;

            if (element.Children.Length > 0)
            {
                foreach (var stype in typeCollections.SerializableTypes)
                {
                    if (stype == type)
                    {
                        // create object
                        var ctor = type.GetConstructor(Array.Empty<Type>());
                        obj = ctor.Invoke(null);

                        // type properties
                        var typeProperties = type.GetProperties().Filter((t) => t.PropertyType != typeof(XmlObject));

                        // iterate children
                        foreach (var child in element.Children)
                        {
                            var childName = child.LocalName;
                            
                            Type childType = typeCollections.GetSerializableType(childName);
                            PropertyInfo property = typeProperties.Find((p) => p.Name == childName);

                            if (childType == null && property != null) childType = property.PropertyType;
                                                       
                            if (property == null && childType != null) property = typeProperties.Find((p) => childType.IsAssignableTo(p.PropertyType));

                            if (property != null && childType != null)
                            {
                                var childObj = Build(child, childType, typeCollections);

                                property.SetValue(obj, childObj);
                            }
                        }

                        break;
                    }
                }
            }
            else if ((o = GetObject(element.InnerHtml, type)) != null) obj = o;

            return obj;
        }

        #endregion
    }



}
