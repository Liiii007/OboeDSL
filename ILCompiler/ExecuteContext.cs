using System;
using System.Runtime.InteropServices;
using OboeCompiler.Calc;

namespace OboeCompiler
{
    public struct ExecuteContext : IDisposable
    {
        private float[]  vars;
        private float[]  cons;
        private float[]  regs;
        private GCHandle varsHandle;
        private GCHandle consHandle;
        private GCHandle regsHandle;

        public ExecuteContext(float[] vars, float[] cons, float[] regs)
        {
            this.vars = vars;
            this.cons = cons;
            this.regs = regs;

            varsHandle = GCHandle.Alloc(vars, GCHandleType.Pinned);
            consHandle = GCHandle.Alloc(cons, GCHandleType.Pinned);
            regsHandle = GCHandle.Alloc(regs, GCHandleType.Pinned);
        }

        public bool IsValueType(MemPos pos)
        {
            return pos.Type == MemPos.MemType.Const ||
                   pos.Type == MemPos.MemType.Reg   ||
                   pos.Type == MemPos.MemType.Var;
        }

        public IntPtr GetPtr(MemPos pos)
        {
            switch (pos.Type)
            {
                case MemPos.MemType.Const:
                    if (pos.Index >= cons.Length) throw new IndexOutOfRangeException();
                    return consHandle.AddrOfPinnedObject() + pos.Index * sizeof(float);
                case MemPos.MemType.Reg:
                    if (pos.Index >= regs.Length) throw new IndexOutOfRangeException();
                    return regsHandle.AddrOfPinnedObject() + pos.Index * sizeof(float);
                case MemPos.MemType.Var:
                    if (pos.Index >= vars.Length) throw new IndexOutOfRangeException();
                    return varsHandle.AddrOfPinnedObject() + pos.Index * sizeof(float);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Dispose()
        {
            varsHandle.Free();
            consHandle.Free();
            regsHandle.Free();
        }

        public static ExecuteContext GetExecuteContext(OboeBackend generator)
        {
            var vars      = new float[generator.VariableIndex.Count];
            var constants = generator.Constants.ToArray();
            var regs      = new float[generator.maxRegUsage];
            var context   = new ExecuteContext(vars, constants, regs);

            return context;
        }
    }
}