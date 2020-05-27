using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace PSCompiler
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    unsafe public struct Variant
    {
        public const ushort c_null = 0x7FF0;
        public const int Size = 8; //
        public enum VarType : ushort
        {
            STR,
            ARR,
            DICT,
            NULL
        }

        [FieldOffset(0)]
        public double dValue;

        [FieldOffset(0)]
        public long lValue;

        [FieldOffset(0)]
        public ulong ulValue;

        [FieldOffset(0)]
        public uint uValue0;
        [FieldOffset(4)]
        public uint uValue1;

        [FieldOffset(0)]
        public int nValue0;
        [FieldOffset(4)]
        public int nValue1;

        [FieldOffset(0)]
        public ushort usValue0;
        [FieldOffset(2)]
        public ushort usValue1;
        [FieldOffset(4)]
        public ushort usValue2;
        [FieldOffset(6)]
        public ushort usValue3;

        [FieldOffset(0)]
        public byte bValue0;
        [FieldOffset(1)]
        public byte bValue1;
        [FieldOffset(2)]
        public byte bValue2;
        [FieldOffset(3)]
        public byte bValue3;
        [FieldOffset(4)]
        public byte bValue4;
        [FieldOffset(5)]
        public byte bValue5;
        [FieldOffset(6)]
        public byte bValue6;
        [FieldOffset(7)]
        public byte bValue7;

        [FieldOffset(8)]
        public void* pValue;

        public Variant(double val) : this()
        {
            pValue = null; // 8 byte double
            dValue = val;  // 8 byte ptr
        }

        public Variant(string str) : this()
        {
            usValue0 = c_null; // 2 byte
            usValue1 = (ushort)VarType.STR; // 2 byte
            nValue1 = str.Length; // 4 bytes
            fixed (char* cstr = str.ToCharArray())
            {
                pValue = (void*)cstr; // 8 byte ptr
            }
        }

        public Variant(char[] str) : this()
        {
            usValue0 = c_null; // 2 byte
            usValue1 = (ushort)VarType.STR; // 2 byte
            nValue1 = str.Length; // 4 bytes
            fixed (char* p = str)
            {
                pValue = (void*)p; // 8 byte ptr
            }
        }

        public Variant(Variant[] arr) : this()
        {
            usValue0 = c_null; // 2 byte
            usValue1 = (ushort)VarType.ARR; // 2 byte
            nValue1 = arr.Length;  // 4 bytes
            fixed (Variant* parr = arr)
            {
                pValue = (void*)parr; // 8 byte ptr
            }
        }

        public Variant(Variant var) : this()
        {
            pValue = var.pValue;
            dValue = var.dValue;
        }

        public static readonly Variant s_null = new Variant { usValue0 = c_null, pValue = null };

        public static Variant Parse(string str)
        {
            if (str[0] == '\'')
            {
                if (str[str.Length - 1] != '\'')
                {
                    throw new Exception("string ending error");
                }

                return new Variant(str.Substring(1, str.Length - 1).Substring(0, str.Length - 2));
            }
            else if (str[0] == '\"')
            {
                if (str[str.Length - 1] != '\"')
                {
                    throw new Exception("string ending error");
                }

                return new Variant(str.Substring(1, str.Length - 1).Substring(0, str.Length - 2));
            }
            else if (str[0] == '{')
            {
                if (str[str.Length - 1] != '}')
                {
                    throw new Exception("list ending error");
                }

                List<Variant> vars = new List<Variant>();
                string[] strVars = str.Substring(1, str.Length - 1).Substring(0, str.Length - 2).Split('|');

                for (int i = 0; i < strVars.Length; ++i)
                {
                    vars.Add(Variant.Parse(strVars[i]));
                }

                return new Variant(vars.ToArray());
            }
            else
            {
                return new Variant(double.Parse(str));
            }

            return s_null;
        }

        public override string ToString()
        {
            if (usValue0 != c_null)
            {
                return dValue.ToString();
            }
            else if ((VarType)usValue1 == VarType.STR)
            {
                return new string((char*)pValue);
            }
            else if ((VarType)usValue1 == VarType.ARR)
            {
                string str = "";

                for (int i = 0; i < nValue1; ++i)
                {
                    str = str + ((Variant*)pValue)[i].ToString() + "|";
                }

                return str;
            }
            else if (pValue == null)
            {
                return "NULL";
            }

            return null;
        }

        public byte[] ToBytes()
        {
            byte[] bytes;

            if (usValue0 == c_null && pValue != null)
            {
                if (usValue1 == (ushort)VarType.STR)
                {
                    int strSize = sizeof(char) * nValue1;
                    bytes = new byte[sizeof(double) + strSize];
                    fixed (byte* p = bytes)
                    {
                        Buffer.MemoryCopy(pValue, (void*)((double*)p + 1), strSize, strSize);
                    }
                }
                else if (usValue1 == (ushort)VarType.ARR)
                {
                    int size = sizeof(double);
                    bytes = new byte[size];
                    for (int i = 0; i < nValue1; ++i)
                    {
                        byte[] varBytes = ((Variant*)pValue)[i].ToBytes();
                        byte[] buf = bytes;
                        bytes = new byte[size + varBytes.Length];
                        Buffer.BlockCopy(buf, 0, bytes, 0, size);
                        Buffer.BlockCopy(varBytes, 0, bytes, size, varBytes.Length);
                        size += varBytes.Length;
                    }
                }
            }
            else
            {
                bytes = new byte[sizeof(double)];
            }

            fixed (byte* p = bytes)
            {
                *((double*)p) = dValue;
            }

            return bytes;
        }

        public static Variant FromBytes(byte** ppc)
        {
            Variant var = new Variant();
            var.dValue = *((double*)*ppc);
            *ppc += sizeof(double);

            if (var.usValue0 == c_null)
            {
                if ((VarType)var.usValue1 == VarType.STR)
                {
                    fixed (char* str = new char[var.nValue1])
                    {
                        int strSize = var.nValue1 * sizeof(char);
                        Buffer.MemoryCopy(*ppc, str, strSize, strSize);
                        *ppc += strSize;
                        var.pValue = (void*)str;
                    }
                }
                else if ((VarType)var.usValue1 == VarType.ARR)
                {
                    fixed (Variant* arr = new Variant[var.nValue1])
                    {
                        for (Variant* p = arr; p < arr + var.nValue1; ++p)
                        {
                            *p = Variant.FromBytes(ppc);
                        }

                        var.pValue = (void*)arr;
                    }
                }
                else
                {
                    var.pValue = null;
                }
            }
            else
            {
                var.pValue = null;
            }

            return var;
        }

        public static bool Equal(Variant* op1, Variant* op2)
        {
            if (op1->usValue0 != c_null && op2->usValue0 != c_null)
            {
                return op1->dValue == op2->dValue;
            }
            else if ((VarType)op1->usValue1 == VarType.STR && (VarType)op2->usValue1 == VarType.STR)
            {

                if (op1->nValue1 != op2->nValue1)
                {
                    return false;
                }

                for (char* p1 = (char*)op1->pValue, p2 = (char*)op2->pValue; p1 < (char*)op1->pValue + op1->nValue1; ++p1, ++p2)
                {
                    if (*p1 != *p2)
                    {
                        return false;
                    }
                }

                return true;
            }
            else if ((VarType)op1->usValue1 == VarType.ARR && (VarType)op2->usValue1 == VarType.STR)
            {
                if (op1->nValue1 != op2->nValue1)
                {
                    return false;
                }

                for (Variant* p1 = (Variant*)op1->pValue, p2 = (Variant*)op2->pValue; p1 < (Variant*)op1->pValue + op1->nValue1; ++p1, ++p2)
                {
                    if (!Equal(p1, p2))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
