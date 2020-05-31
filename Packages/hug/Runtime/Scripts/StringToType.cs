using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AID
{
    /// <summary>
    /// Static container for working with string data sources to typed sources.
    ///
    /// Uses object boxing to return through the same interface. To add more
    /// types to support, see the converters dictionary. It must register as the
    /// type it supports and then provide a function that takes string and returns
    /// boxed object of stated type.
    /// </summary>
    public static class StringToType
    {
        public delegate object StringToTypeDelegate(string s);

        private static readonly Dictionary<System.Type, StringToTypeDelegate> converters =
            new Dictionary<System.Type, StringToTypeDelegate>()
            {
                {typeof(int)                , (string s) => { return int.Parse(s);} },
                {typeof(float)              , (string s) => { return float.Parse(s);} },
                {typeof(string)             , (string s) => { return s; } },
                {typeof(bool)               , (string s) => { return bool.Parse(s); } },
                {typeof(UnityEngine.Vector4), (string s) => { return V4FromFloatArray(StringToFloatArray(s)); } },
                {typeof(UnityEngine.Vector3), (string s) => { return V3FromFloatArray(StringToFloatArray(s)); } },
                {typeof(UnityEngine.Vector2), (string s) => { return V2FromFloatArray(StringToFloatArray(s)); } }
            };

        /// <summary>
        /// Extracts delimited numberals only
        /// </summary>
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

        public static UnityEngine.Vector4 V4FromFloatArray(float[] f)
        {
            if (f.Length != 4)
                throw new System.Exception("Incorrect number of floats provided for Vector4");

            return new UnityEngine.Vector4(f[0], f[1], f[2], f[3]);
        }

        public static UnityEngine.Vector3 V3FromFloatArray(float[] f)
        {
            if (f.Length != 3)
                throw new System.Exception("Incorrect number of floats provided for Vector3");

            return new UnityEngine.Vector3(f[0], f[1], f[2]);
        }

        public static UnityEngine.Vector2 V2FromFloatArray(float[] f)
        {
            if (f.Length != 2)
                throw new System.Exception("Incorrect number of floats provided for Vector2");

            return new UnityEngine.Vector2(f[0], f[1]);
        }

        public static object TryGetTypeFromString<T>(string s)
        {
            return TryGetTypeFromString(typeof(T), s);
        }

        /// <summary>
        /// Use the converts to return correct value from string in target type
        /// </summary>
        /// <returns>value from input string, default(T) on failure</returns>
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

        public static bool IsSupported<T>()
        {
            return IsSupported(typeof(T));
        }

        /// <summary>
        /// Is the given type listed in the converters.
        /// </summary>
        public static bool IsSupported(System.Type t)
        {
            return converters.ContainsKey(t);
        }
    }
}