// Mini JSON reader, subset vừa đủ schema level. Không JsonUtility (nằm trong UnityEngine →
// phá ràng buộc "Domain không import UnityEngine", lại không đọc được null trong mảng) và
// không thêm Newtonsoft.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WordStack.Board
{
    // Subset: object / array / string / number / true / false / null.
    static class Json
    {
        public static object Parse(string s)
        {
            int i = (s.Length > 0 && s[0] == '﻿') ? 1 : 0;
            var v = Value(s, ref i);
            Ws(s, ref i);
            if (i < s.Length) throw Err(s, i, "còn rác sau giá trị JSON");
            return v;
        }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) throw Err(s, i, "hết chuỗi giữa chừng");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (Lit(s, ref i, "true")) return true;
            if (Lit(s, ref i, "false")) return false;
            if (Lit(s, ref i, "null")) return null;
            return Num(s, ref i);
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++;                                    // '{'
            Ws(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            for (;;)
            {
                Ws(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Err(s, i, "chờ tên field trong dấu nháy kép");
                string k = Str(s, ref i);
                Ws(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Err(s, i, "chờ ':' sau tên field");
                i++;
                d[k] = Value(s, ref i);
                Ws(s, ref i);
                if (i >= s.Length) throw Err(s, i, "object chưa đóng");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return d; }
                throw Err(s, i, "chờ ',' hoặc '}'");
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var l = new List<object>();
            i++;                                    // '['
            Ws(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            for (;;)
            {
                l.Add(Value(s, ref i));
                Ws(s, ref i);
                if (i >= s.Length) throw Err(s, i, "array chưa đóng");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return l; }
                throw Err(s, i, "chờ ',' hoặc ']'");
            }
        }

        static string Str(string s, ref int i)
        {
            i++;                                    // '"'
            var sb = new StringBuilder();
            for (;;)
            {
                if (i >= s.Length) throw Err(s, i, "chuỗi chưa đóng");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw Err(s, i, "escape cụt");
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Err(s, i, "\\u thiếu 4 chữ số hex");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw Err(s, i - 1, "escape lạ '\\" + e + "'");
                }
            }
        }

        static double Num(string s, ref int i)
        {
            int start = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E'
                                    || ((s[i] == '-' || s[i] == '+') && (s[i - 1] == 'e' || s[i - 1] == 'E')))) i++;
            double d;
            if (start == i || !double.TryParse(s.Substring(start, i - start),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                throw Err(s, start, "số không hợp lệ");
            return d;
        }

        static bool Lit(string s, ref int i, string lit)
        {
            if (i + lit.Length > s.Length || string.CompareOrdinal(s, i, lit, 0, lit.Length) != 0) return false;
            i += lit.Length;
            return true;
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        }

        static Exception Err(string s, int i, string msg)
        {
            int line = 1, col = 1;
            for (int k = 0; k < i && k < s.Length; k++) { if (s[k] == '\n') { line++; col = 1; } else col++; }
            return new Exception("JSON lỗi dòng " + line + " cột " + col + ": " + msg);
        }

        // ---- truy cập có kiểm kiểu, message chỉ rõ chỗ hỏng ----
        public static Dictionary<string, object> AsObj(object o, string where)
        {
            var d = o as Dictionary<string, object>;
            if (d == null) throw new Exception(where + ": chờ object");
            return d;
        }

        public static List<object> AsArr(object o, string where)
        {
            var l = o as List<object>;
            if (l == null) throw new Exception(where + ": chờ array");
            return l;
        }

        public static string AsStr(object o, string where)
        {
            if (o == null) return null;
            var s = o as string;
            if (s == null) throw new Exception(where + ": chờ chuỗi");
            return s;
        }

        public static double AsNum(object o, string where)
        {
            if (!(o is double)) throw new Exception(where + ": chờ số");
            return (double)o;
        }

        public static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) ? v : null;
        }
    }
}
