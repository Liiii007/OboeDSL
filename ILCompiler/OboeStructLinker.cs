using System;
using System.Collections.Generic;

namespace OboeCompiler
{
    public struct BindTest : IBindable
    {
        public float a;
        public float b;

        public static void AddId(OboeStructLinker structLinker)
        {
            structLinker.BindId("$bind.a");
            structLinker.BindId("$bind.b");
        }

        public static unsafe void BindValue(BindTest* ptr, OboeStructLinker structLinker)
        {
            structLinker.BindValue("$bind.a", (IntPtr)(&ptr->a));
            structLinker.BindValue("$bind.b", (IntPtr)(&ptr->b));
        }
    }

    public class OboeStructLinker
    {
        public int                     bindCount  = 0;
        public Dictionary<string, int> IdToIndex  = new Dictionary<string, int>();
        public Dictionary<int, IntPtr> IndexToPtr = new Dictionary<int, IntPtr>();

        public List<IntPtr> Ptrs = new List<IntPtr>();

        public void BindType<T>(string rootName)
        {
            foreach (var field in typeof(T).GetFields())
            {
                var varName = rootName + "." + field.Name;
                BindId(varName);
            }
        }

        public void BindId(string varName)
        {
            IdToIndex[varName] = bindCount++;
        }

        public void BindValue(string varName, IntPtr ptr)
        {
            if (IdToIndex.TryGetValue(varName, out var varIndex))
            {
                IndexToPtr[varIndex] = ptr;
            }
            else
            {
                throw new Exception("Not Bind, please bind ID first");
            }
        }
    }

    public interface IBindable { }
}