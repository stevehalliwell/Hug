using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AID
{
    public static class StringToType
    {
        public delegate object StringToTypeDelegate(string s);

        //these are not very exhaustive
        private static readonly Dictionary<System.Type, StringToTypeDelegate> converters =
            new Dictionary<System.Type, StringToTypeDelegate>()
            {
                {typeof(int)    , (string s) => {return int.Parse(s);} },
                {typeof(float)  , (string s) => {return float.Parse(s);} },
                {typeof(string)  , (string s) => {return s; } },
                {typeof(bool)  , (string s) => {return bool.Parse(s); } },
                {typeof(UnityEngine.Vector4)  , (string s) => { return V4FromFloatArray(StringToFloatArray(s)); } },
                {typeof(UnityEngine.Vector3)  , (string s) => { return (UnityEngine.Vector3) V4FromFloatArray(StringToFloatArray(s)); } },
                {typeof(UnityEngine.Vector2)  , (string s) => { return (UnityEngine.Vector2) V4FromFloatArray(StringToFloatArray(s)); } }
            };

        public static float[] StringToFloatArray(string s)
        {
            Regex regex = new Regex(@"-?(?:\d*\.)?\d+");
            MatchCollection matches = regex.Matches(s);
            float[] res = new float[matches.Count];
            for (int i = 0; i < res.Length; i++)
            {
                try
                {
                    res[i] = float.Parse(matches[i].Value);
                }
                catch (System.Exception)
                {
                }
            }
            return res;
        }

        public static UnityEngine.Vector4 V4FromFloatArray(float[] floats)
        {
            var retval = new UnityEngine.Vector4();

            if (floats.Length > 0)
                retval.x = floats[0];
            if (floats.Length > 1)
                retval.y = floats[1];
            if (floats.Length > 2)
                retval.z = floats[2];
            if (floats.Length > 3)
                retval.w = floats[3];

            return retval;
        }

        //very stupid and simple now but later may need to support heirarchy
        public static string[] ParamStringToElements(string s)
        {
            //thx http://regexr.com/
            Regex regex = new Regex(@"\(.*?\)|\[.*?\]|"".*?""|[^\s]+");
            MatchCollection matches = regex.Matches(s);
            string[] res = new string[matches.Count];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = matches[i].Value;
            }
            return res;
        }

        public static bool ParamStringToObjects(string s, ParameterInfo[] pList, out object[] out_params)
        {
            var sParams = ParamStringToElements(s);

            bool hasSucceded = true;
            out_params = new object[pList.Length];

            if (sParams.Length != pList.Length)
            {
                UnityEngine.Debug.LogError("Param count mismatch. Expected " + pList.Length.ToString() + " got " + sParams.Length);

                hasSucceded = false;
            }
            else
            {
                for (int i = 0; i < pList.Length; i++)
                {
                    var res = TryGetTypeFromString(pList[i].ParameterType, sParams[i]);

                    if (res == null)
                    {
                        hasSucceded = false;
                        UnityEngine.Debug.LogError(string.Format("Param #{0} failed. Could not convert \"{1}\" to type {2}", i, sParams[i], pList[i].ParameterType.Name));
                    }

                    out_params[i] = res;
                }
            }

            return hasSucceded;
        }

        public static object TryGetTypeFromString(System.Type t, string s)
        {
            object retval = null;

            try
            {
                if (converters.TryGetValue(t, out var del))
                {
                    retval = del(s);
                }
            }
            catch (System.Exception)
            {
                UnityEngine.Debug.LogError("Failed to convert param. Got " + s + " could not convert to " + t.Name);
            }

            return retval;
        }

        public static bool Supports(System.Type t)
        {
            return converters.ContainsKey(t);
        }

        public static bool Supports(ParameterInfo[] pInfo, out string report)
        {
            bool success = true;
            report = string.Empty;
            for (int i = 0; i < pInfo.Length; i++)
            {
                if (!converters.TryGetValue(pInfo[i].ParameterType, out var del))
                {
                    success = false;
                    report += string.Format("Param #{0} \"{1}\" is not supported by StringToType", i, pInfo[i].ParameterType.Name);
                }
            }

            return success;
        }
    }
}