﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EProjectFile
{
    class StructInfo:IHasId
    {
        private int id;
        public int Id => id;
        public StructInfo(int id)
        {
            this.id = id;
        }
        [JsonIgnore]
        public int UnknownAfterId;
        public int Flags;
        public string Name;
        public string Comment;
        public VariableInfo[] Member;
        public static StructInfo[] ReadStructs(BinaryReader reader)
        {
            var headerSize = reader.ReadInt32();
            int count = headerSize / 8;
            var ids = reader.ReadInt32sWithFixedLength(count);
            var unknownsAfterIds = reader.ReadInt32sWithFixedLength(count);
            var structs = new StructInfo[count];
            for (int i = 0; i < count; i++)
            {
                var structInfo = new StructInfo(ids[i])
                {
                    UnknownAfterId = unknownsAfterIds[i],
                    Flags = reader.ReadInt32(),
                    Name = reader.ReadStringWithLengthPrefix(),
                    Comment = reader.ReadStringWithLengthPrefix(),
                    Member = VariableInfo.ReadVariables(reader)
                };
                structs[i] = structInfo;
            }

            return structs;
        }
        public static void WriteStructs(BinaryWriter writer, StructInfo[] structs)
        {
            writer.Write(structs.Length * 8);
            Array.ForEach(structs, x => writer.Write(x.Id));
            Array.ForEach(structs, x => writer.Write(x.UnknownAfterId));
            foreach (var structInfo in structs)
            {
                writer.Write(structInfo.Flags);
                writer.WriteStringWithLengthPrefix(structInfo.Name);
                writer.WriteStringWithLengthPrefix(structInfo.Comment);
                VariableInfo.WriteVariables(writer, structInfo.Member);
            }
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
