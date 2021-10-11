using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public interface ITypeRepository
    {
        bool IsSerializable(Type type);
    }


    public class TypeRepository : ITypeRepository
    {
        #region Fields

        public Type[] Types { get; }

        private Type[] serializableTypes = null;

        public Type[] SerializableTypes
        {
            get
            {
                if (serializableTypes == null)
                {
                    var stypes = new List<Type>();

                    foreach (var type in Types)
                    {
                        var ok = true;
                        ok = ok && type.GetConstructor(Type.EmptyTypes) != null;
                        if (ok) stypes.Add(type);
                    }

                    serializableTypes = stypes.ToArray();
                }

                return serializableTypes;
            }
        }

        #endregion

        #region Constructors

        public TypeRepository(Type[] types)
        {
            Types = types;
        }

        #endregion

        #region Methods

        public Type GetSerializableType(string name)
        {
            foreach (var type in SerializableTypes)
            {
                if (type.Name == name)
                    return type;
            }
            return null;
        }

        public bool IsSerializable(Type type)
        {
            Type checkType = null;
            if (type.IsArray)
                checkType = type.GetElementType();
            else
                checkType = type;

            return SerializableTypes.Contains(checkType);
        }

        #endregion
    }
}
