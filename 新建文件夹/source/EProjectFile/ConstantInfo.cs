﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EProjectFile
{
    class ConstantInfo : IHasId
    {
        private class ConstantValueConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                switch(reader.TokenType)
                {
                    case JsonToken.Boolean:
                    case JsonToken.Float:
                    case JsonToken.Integer:
                    case JsonToken.String:
                        return reader.Value;
                }
                if(reader.TokenType != JsonToken.StartObject)
                {
                    throw new Exception();
                }
                object value = null;
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.PropertyName:
                            if(value != null)
                            {
                                throw new Exception();
                            }
                            var keyName = (string)reader.Value;
                            reader.Read();
                            if ("bytes".Equals(keyName))
                            {
                                value = new HexConverter().ReadJson(reader, typeof(byte[]), null, serializer);
                            }
                            else if("date".Equals(keyName))
                            {
                                value = new IsoDateTimeConverter().ReadJson(reader, typeof(byte[]), null, serializer);
                            }
                            else
                            {
                                throw new Exception();
                            }
                            break;
                        case JsonToken.EndObject:
                            return value;
                        case JsonToken.Comment:
                            break;
                        default:
                            throw new Exception();
                    }
                }
                throw new Exception();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if(value is byte[])
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("bytes");
                    new HexConverter().WriteJson(writer, value, serializer);
                    writer.WriteEndObject();
                }
                else if(value is DateTime)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("date");
                    new IsoDateTimeConverter().WriteJson(writer, value, serializer);
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteValue(value);
                }
            }
        }


        private int id;
        public int Id => id;
        public ConstantInfo(int id)
        {
            this.id = id;
        }

        public int Flags;//1=未验证（此时<Value is string>）
        public string Name;
        public string Comment;
        [JsonConverter(typeof(ConstantValueConverter))]
        public Object Value;

        public static ConstantInfo[] ReadConstants(BinaryReader r)
        {
            return r.ReadBlocksWithIdAndOffest((reader, id) =>
                {
                    var constant = new ConstantInfo(id)
                    {
                        Flags = reader.ReadInt16(),
                        Name = reader.ReadCStyleString(),
                        Comment = reader.ReadCStyleString()
                    };
                    switch (unchecked((uint)id) >> 28)
                    {
                        case 1://常量
                            byte type = reader.ReadByte();
                            switch (type)
                            {
                                case 22:
                                    constant.Value = null;
                                    break;
                                case 23:
                                    constant.Value = reader.ReadDouble();
                                    break;
                                case 24:
                                    constant.Value = reader.ReadInt32() != 0;
                                    break;
                                case 25:
                                    constant.Value = DateTime.FromOADate(reader.ReadDouble());
                                    break;
                                case 26:
                                    int endOfBlock = (int)reader.BaseStream.Position + reader.ReadInt32() + 4;
                                    constant.Value = reader.ReadCStyleString();
                                    reader.BaseStream.Position = endOfBlock;
                                    break;
                                default:
                                    throw new Exception();
                            }
                            break;
                        case 2://图片
                        case 3://声音
                            constant.Value = reader.ReadBytesWithLengthPrefix();
                            break;
                        default:
                            throw new Exception();
                    }
                    return constant;
                }
            );
        }
        public static void WriteConstants(BinaryWriter w, ConstantInfo[] constants)
        {
            w.WriteBlocksWithIdAndOffest(constants, (writer, elem) =>
            {
                writer.Write((short)elem.Flags);
                writer.WriteCStyleString(elem.Name);
                writer.WriteCStyleString(elem.Comment);
                if (elem.Value is byte[])
                {
                    writer.WriteBytesWithLengthPrefix((byte[])elem.Value);
                }
                else if (elem.Value == null)
                {
                    writer.Write((byte)22);
                }
                else if (elem.Value is double)
                {
                    writer.Write((byte)23);
                    writer.Write((double)elem.Value);
                }
                else if (elem.Value is bool)
                {
                    writer.Write((byte)24);
                    writer.Write(((bool)elem.Value) ? 1 : 0);
                }
                else if (elem.Value is DateTime)
                {
                    writer.Write((byte)25);
                    writer.Write(((DateTime)elem.Value).ToOADate());
                }
                else if (elem.Value is string)
                {
                    byte[] block;
                    using (var blockWriter = new BinaryWriter(new MemoryStream()))
                    {
                        blockWriter.WriteCStyleString((string)elem.Value);
                        block = ((MemoryStream)blockWriter.BaseStream).ToArray();
                    }
                    writer.Write((byte)26);
                    writer.WriteBytesWithLengthPrefix(block);
                }
                else
                {
                    throw new Exception();
                }
            });
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
