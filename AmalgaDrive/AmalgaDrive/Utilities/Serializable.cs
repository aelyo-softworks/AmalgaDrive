using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Utilities
{
    public abstract class Serializable<T> : IDataErrorInfo where T : new()
    {
        public static string DefaultConfigurationFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), typeof(T).Namespace, typeof(T).Name + ".config");

        protected virtual void Validate(IList<string> errors, string memberName)
        {
        }

        protected virtual string Validate(string memberName)
        {
            var errors = new List<string>();
            Validate(errors, memberName);
            if (errors.Count == 0)
                return null;

            return string.Join(Environment.NewLine, errors);
        }

        string IDataErrorInfo.Error => Validate(null);
        string IDataErrorInfo.this[string columnName] => Validate(columnName);

        public static T Deserialize(string filePath) => Deserialize(filePath, new T());
        public static T Deserialize(string filePath, T defaultValue)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                return defaultValue;

            try
            {
                using (var reader = new XmlTextReader(filePath))
                {
                    return Deserialize(reader, defaultValue);
                }
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize file '" + filePath + "': " + e);
                return defaultValue;
            }
#else
            catch
            {
                return defaultValue;
            }
#endif
        }

        public static T Deserialize(TextReader reader, T defaultValue, bool throwOnError)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (throwOnError)
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize textreader: " + e);
                return defaultValue;
            }
        }

        public static T Deserialize(XmlReader reader, T defaultValue, bool throwOnError)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (throwOnError)
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize xmlreader: " + e);
                return defaultValue;
            }
#else
            catch
            {
                return defaultValue;
            }
#endif
        }

        public static T Deserialize(string filePath, T defaultValue, bool throwOnError)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                return defaultValue;

            if (throwOnError)
            {
                using (var reader = new XmlTextReader(filePath))
                {
                    return Deserialize(reader, defaultValue, throwOnError);
                }
            }

            try
            {
                using (var reader = new XmlTextReader(filePath))
                {
                    return Deserialize(reader, defaultValue, throwOnError);
                }
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize file '" + filePath + "': " + e);
                return defaultValue;
            }
#else
            catch
            {
                return defaultValue;
            }
#endif
        }

        public static T Deserialize(Stream stream) => Deserialize(stream, new T());
        public static T Deserialize(Stream stream, T defaultValue)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(stream);
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize stream: " + e);
                return defaultValue;
            }
#else
            catch
            {
                return defaultValue;
            }
#endif
        }

        public static T Deserialize(Stream stream, T defaultValue, bool throwOnError)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (throwOnError)
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(stream);
            }

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(stream);
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize stream: " + e);
                return defaultValue;
            }
#else
            catch
            {
                return defaultValue;
            }
#endif
        }

        public static T Deserialize(TextReader reader, T defaultValue)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize textreader: " + e);
                return defaultValue;
            }
#else
            catch
            {
                return defaultValue;
            }
#endif
        }

        public static T Deserialize(XmlReader reader) => Deserialize(reader, new T());
        public static T Deserialize(XmlReader reader, T defaultValue)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("!!!Exception trying to deserialize xmlreader: " + e);
                return defaultValue;
            }
#else
            catch
            {
                return defaultValue;
            }
#endif
        }

        public virtual void Serialize(XmlWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var serializer = new XmlSerializer(GetType());
            serializer.Serialize(writer, this);
        }

        public virtual void Serialize(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var serializer = new XmlSerializer(GetType());
            serializer.Serialize(writer, this);
        }

        public virtual void Serialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var serializer = new XmlSerializer(GetType());
            serializer.Serialize(stream, this);
        }

        public virtual void Serialize(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            IOUtilities.FileCreateDirectory(filePath);
            using (var writer = new XmlTextWriter(filePath, Encoding.UTF8))
            {
                Serialize(writer);
            }
        }
    }
}
