using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Aveezo
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class FieldAttribute : Attribute
    {
        public string Name { get; private set; }

        public bool Updatable { get; private set; }

        public FieldAttribute(string name, bool updatable)
        {
            Name = name;
            Updatable = updatable;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DataAttribute : Attribute
    {
        public string Name { get; private set; }

        public string Id { get; private set; }

        public string Ident { get; private set; }

        public DataAttribute(string name, string id, string ident)
        {
            Name = name;            
            Id = id;
            Ident = ident;
        }
    }
    
    public abstract class Data
    {
        #region Fields

        public string Id { get; set; }

        public List<PropertyInfo> Updates { get; set; } = null;

        #endregion

        #region Methods

        public bool IsUpdated(string propertyName)
        {
            if (Updates == null)
            {
                return false;
            }
            else
            {
                bool found = false;

                foreach (PropertyInfo property in Updates)
                {
                    if (property.Name == propertyName)
                    {
                        found = true;
                        break;
                    }
                }


                return found;
            }
        }

        public bool Update<T>(out T update, Row row) where T : Data, new()
        {
            update = null;

            object[] dataAttributes = typeof(T).GetCustomAttributes(false);

            if (dataAttributes.Length > 0)
            {
                DataAttribute dataAttribute = dataAttributes[0] as DataAttribute;

                if (dataAttribute != null)
                {
                    string name = dataAttribute.Name;
                    string id = dataAttribute.Id;
                    string ident = dataAttribute.Ident;

                    if (name != null && id != null)
                    {
                        bool updateReturn = false;

                        update = new T();

                        string idValue = row[id];

                        update.Id = idValue;
                        Id = idValue;

                        PropertyInfo[] properties = typeof(T).GetProperties();

                        List<PropertyInfo> lupdates = new List<PropertyInfo>();

                        foreach (PropertyInfo property in properties)
                        {
                            string propertyName = property.Name;

                            if (propertyName == "Id")
                            {
                            }
                            else if (propertyName == "Updates")
                            {
                                property.SetValue(update, lupdates);
                            }
                        }

                        foreach (PropertyInfo property in properties)
                        {
                            string propertyName = property.Name;

                            if (propertyName == "Id" || propertyName == "Updates")
                            {
                            }
                            else
                            {
                                object[] fieldAttributes = property.GetCustomAttributes(true);

                                if (fieldAttributes.Length > 0)
                                {
                                    FieldAttribute fieldAttribute = fieldAttributes[0] as FieldAttribute;

                                    if (fieldAttribute != null)
                                    {
                                        string fieldName = fieldAttribute.Name;
                                        bool fieldUpdatable = fieldAttribute.Updatable;

                                        string fieldFormatedName;

                                        if (ident != null)
                                        {
                                            fieldFormatedName = string.Format(fieldName, ident);
                                        }
                                        else
                                        {
                                            fieldFormatedName = fieldName;
                                        }

                                        if (fieldUpdatable && row.ContainsKey(fieldFormatedName))
                                        {
                                            Column column = row[fieldFormatedName];                                            
                                            object current = property.GetValue(this);

                                            Type currentType = property.PropertyType;

                                            bool valueUpdated = false;

                                            if (currentType == typeof(int?))
                                            {
                                                if (column != (int?)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(int))
                                            {
                                                if (column != (int)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(string))
                                            {
                                                if (column.ToString() != (string)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(char?))
                                            {
                                                if (column != (char?)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(char))
                                            {
                                                if (column != (char)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(long?))
                                            {
                                                if (column != (long?)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(long))
                                            {
                                                if (column != (long)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(double?))
                                            {
                                                if (column != (double?)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(double))
                                            {
                                                if (column != (double)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(float?))
                                            {
                                                if (column != (float?)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(float))
                                            {
                                                if (column != (float)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(bool?))
                                            {
                                                if (column != (bool?)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(bool))
                                            {
                                                if (column != (bool)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(DateTime?))
                                            {
                                                if (column != (DateTime?)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }
                                            else if (currentType == typeof(DateTime))
                                            {
                                                if (column != (DateTime)current)
                                                {
                                                    valueUpdated = true;
                                                }
                                            }

                                            if (valueUpdated)
                                            {
                                                updateReturn = true;
                                                property.SetValue(update, current);
                                                lupdates.Add(property);
                                            }

                                            //if (db["NC_Info2"].ToString() != li.Info2)
                                            //{
                                            //    update = true;
                                            //    u.UpdateInfo2 = true;
                                            //    u.Info2 = li.Info2;
                                            //    UpdateInfo(updateinfo, "info2", db["NC_Info2"].ToString(), li.Info2);
                                            //}
                                        }
                                    }
                                }
                            }
                        }

                        return updateReturn;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
                return false;
        }

        #endregion
    }
}
