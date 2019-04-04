﻿//  Copyright 2019 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using NtApiDotNet.Utilities.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NtApiDotNet.Ndr
{
#pragma warning disable 1591
    /// <summary>
    /// A buffer to unmarshal NDR data from.
    /// </summary>
    /// <remarks>This class is primarily for internal use only.</remarks>
    public class NdrUnmarshalBuffer
    {
        private readonly MemoryStream _stm;
        private readonly BinaryReader _reader;
        private readonly List<NtObject> _handles;

        private static int CaclulateAlignment(int offset, int alignment)
        {
            int result = alignment - (offset % alignment);
            if (result < alignment)
            {
                return result;
            }
            return 0;
        }

        public void Align(int alignment)
        {
            _stm.Position += CaclulateAlignment((int)_stm.Position, alignment);
        }

        public NdrUnmarshalBuffer(byte[] buffer, IEnumerable<NtObject> handles)
        {
            _stm = new MemoryStream(buffer);
            _reader = new BinaryReader(_stm, Encoding.Unicode);
            _handles = new List<NtObject>(handles.Select(o => o.DuplicateObject()));
        }

        public byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public byte[] ReadBytes(int count)
        {
            byte[] ret = _reader.ReadBytes(count);
            if (ret.Length < count)
            {
                throw new EndOfStreamException();
            }
            return ret;
        }

        public char[] ReadChars(int count)
        {
            char[] chars = _reader.ReadChars(count);
            if (chars.Length < count)
            {
                throw new EndOfStreamException();
            }
            return chars;
        }

        public sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        public short ReadInt16()
        {
            Align(2);
            return _reader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            Align(2);
            return _reader.ReadUInt16();
        }

        public int ReadInt32()
        {
            Align(4);
            return _reader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            Align(4);
            return _reader.ReadUInt32();
        }

        public long ReadInt64()
        {
            Align(8);
            return _reader.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            Align(8);
            return _reader.ReadUInt64();
        }

        public IntPtr ReadIntPtr()
        {
            return new IntPtr(ReadInt32());
        }

        public UIntPtr ReadUIntPtr()
        {
            return new UIntPtr(ReadUInt32());
        }

        public float ReadFloat()
        {
            Align(4);
            return _reader.ReadSingle();
        }

        public double ReadDouble()
        {
            Align(8);
            return _reader.ReadDouble();
        }

        public int ReadReferent()
        {
            return ReadInt32();
        }

        public string ReadFixedString(int count)
        {
            return new string(ReadChars(count));
        }

        public string ReadAnsiConformantString()
        {
            int max_count = ReadInt32();
            int offset = ReadInt32();
            int actual_count = ReadInt32();

            return BinaryEncoding.Instance.GetString(_reader.ReadBytes(actual_count)).TrimEnd('\0');
        }

        public string ReadConformantString()
        {
            int max_count = ReadInt32();
            int offset = ReadInt32();
            int actual_count = ReadInt32();

            return new string(_reader.ReadChars(actual_count)).TrimEnd('\0');
        }

        public T ReadUniquePointer<T>(Func<T> read_func) where T : class
        {
            int referent = ReadInt32();
            if (referent == 0)
            {
                return null;
            }
            return read_func();
        }

        public Guid ReadGuid()
        {
            Align(4);
            return new Guid(ReadBytes(16));
        }

        public T ReadStruct<T>() where T : INdrStructure, new()
        {
            T ret = new T();
            ret.Unmarshal(this);
            return ret;
        }

        public T ReadHandle<T>() where T : NtObject
        {
            int index = ReadInt32();
            if (!NtObjectUtils.IsWindows81OrLess)
            {
                // Unsure what this is on Windows 10. This isn't used on Windows 8.X.
                ReadInt32();
            }

            return (T)_handles[index - 1].DuplicateObject();
        }
    }
#pragma warning restore 1591
}
