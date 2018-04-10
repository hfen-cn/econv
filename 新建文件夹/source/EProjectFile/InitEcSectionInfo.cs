﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EProjectFile
{
    class InitEcSectionInfo
    {

        public const string SectionName = "初始模块段";
        public string[] EcName;
        public int[] InitMethod;
        public static InitEcSectionInfo Parse(byte[] data)
        {
            var initEcSectionInfo = new InitEcSectionInfo();
            using (var reader = new BinaryReader(new MemoryStream(data, false)))
            {
                initEcSectionInfo.EcName = reader.ReadStringsWithMfcStyleCountPrefix();
                initEcSectionInfo.InitMethod = reader.ReadInt32sWithFixedLength(reader.ReadInt32() / 4);
            }
            return initEcSectionInfo;
        }

        public byte[] ToBytes()
        {
            byte[] data;
            using (var writer = new BinaryWriter(new MemoryStream()))
            {
                WriteTo(writer);
                writer.Flush();
                data = ((MemoryStream)writer.BaseStream).ToArray();
            }
            return data;
        }
        public void WriteTo(BinaryWriter writer)
        {
            writer.WriteStringsWithMfcStyleCountPrefix(EcName);
            writer.Write(InitMethod.Length * 4);
            writer.WriteInt32sWithoutLengthPrefix(InitMethod);
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
