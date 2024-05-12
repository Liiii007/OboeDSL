using System;
using System.Runtime.CompilerServices;
using OboeCompiler;
using OboeCompiler.Calc;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct OboeBurstVM : IJob
{
    [ReadOnly] public NativeArray<Instruction> Instructions;

    public void Execute()
    {
        int pcRegister = 0;
        int length     = Instructions.Length;

        while (pcRegister < length)
        {
            var instr = Instructions[pcRegister];
            switch (instr.Type)
            {
                case InstructionType.None:
                    break;
                case InstructionType.Add:
                    AddOp(instr);
                    break;
                case InstructionType.Sub:
                    SubOp(instr);
                    break;
                case InstructionType.Mul:
                    MulOp(instr);
                    break;
                case InstructionType.Div:
                    DivOp(instr);
                    break;
                case InstructionType.Mod:
                    break;
                case InstructionType.Store:
                    StoreOp(instr);
                    break;
                case InstructionType.Jump:
                    JumpOp(instr, ref pcRegister);
                    break;
                case InstructionType.TrueAndJump:
                    TrueAndJumpOp(instr, ref pcRegister);
                    break;
                case InstructionType.Larger:
                    LargerOp(instr);
                    break;
                case InstructionType.LargerEqual:
                    LargerEqualOp(instr);
                    break;
                case InstructionType.Equal:
                    EqualOp(instr);
                    break;
                case InstructionType.NotEqual:
                    NotEqualOp(instr);
                    break;
                case InstructionType.Sin:
                    SinOp(instr);
                    break;
                case InstructionType.Cos:
                    CosOp(instr);
                    break;
                case InstructionType.Tan:
                    TanOp(instr);
                    break;
                case InstructionType.Sqrt:
                    SqrtOp(instr);
                    break;
            }

            pcRegister++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LoadMemPos(IntPtr memPos)
    {
        float* ptr = (float*)memPos;
        return *ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StoreMemPos(IntPtr memPos, float value)
    {
        float* ptr = (float*)memPos;
        *ptr = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);
        StoreMemPos(instruction.Dst.Ptr, src0 + src1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SubOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);
        StoreMemPos(instruction.Dst.Ptr, src0 - src1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MulOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);
        StoreMemPos(instruction.Dst.Ptr, src0 * src1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DivOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);
        StoreMemPos(instruction.Dst.Ptr, src0 / src1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ModOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);
        StoreMemPos(instruction.Dst.Ptr, src0 % src1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SinOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        StoreMemPos(instruction.Dst.Ptr, math.sin(src0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CosOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        StoreMemPos(instruction.Dst.Ptr, math.cos(src0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TanOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        StoreMemPos(instruction.Dst.Ptr, math.tan(src0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SqrtOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        StoreMemPos(instruction.Dst.Ptr, math.sqrt(src0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StoreOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        StoreMemPos(instruction.Dst.Ptr, src0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LargerOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);

        StoreMemPos(instruction.Dst.Ptr, src0 > src1 ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LargerEqualOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);

        StoreMemPos(instruction.Dst.Ptr, src0 >= src1 ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EqualOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);

        StoreMemPos(instruction.Dst.Ptr, Math.Abs(src0 - src1) < 10e-6 ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotEqualOp(Instruction instruction)
    {
        float src0 = LoadMemPos(instruction.Src0.Ptr);
        float src1 = LoadMemPos(instruction.Src1.Ptr);

        StoreMemPos(instruction.Dst.Ptr, Math.Abs(src0 - src1) >= 10e-6 ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TrueAndJumpOp(Instruction instruction, ref int pc)
    {
        float boolValue = LoadMemPos(instruction.Dst.Ptr);

        if (boolValue != 0)
        {
            pc = instruction.Src0.Index - 1;
        }
        else
        {
            pc = instruction.Src1.Index - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void JumpOp(Instruction instruction, ref int pc)
    {
        pc = instruction.Src0.Index - 1;
    }

    #region Bind

    public static void LinkExternal(NativeArray<Instruction> generatorInstructions, OboeStructLinker linker)
    {
        for (int i = 0; i < generatorInstructions.Length; i++)
        {
            var ins = generatorInstructions[i];
            ins                      = LinkExternal(linker, ins);
            generatorInstructions[i] = ins;
        }
    }

    private static Instruction LinkExternal(OboeStructLinker linker, Instruction ins)
    {
        if (ins.Src0.Type == MemPos.MemType.External) ins.Src0.Ptr = linker.IndexToPtr[ins.Src0.Index];
        if (ins.Src1.Type == MemPos.MemType.External) ins.Src1.Ptr = linker.IndexToPtr[ins.Src1.Index];
        if (ins.Dst.Type  == MemPos.MemType.External) ins.Dst.Ptr  = linker.IndexToPtr[ins.Dst.Index];
        return ins;
    }

    public static void LinkContext(NativeArray<Instruction> generatorInstructions, ExecuteContext context)
    {
        for (int i = 0; i < generatorInstructions.Length; i++)
        {
            var ins = generatorInstructions[i];
            ins                      = LinkContext(context, ins);
            generatorInstructions[i] = ins;
        }
    }

    private static Instruction LinkContext(ExecuteContext context, Instruction ins)
    {
        if (context.IsValueType(ins.Src0)) ins.Src0.Ptr = context.GetPtr(ins.Src0);
        if (context.IsValueType(ins.Src1)) ins.Src1.Ptr = context.GetPtr(ins.Src1);
        if (context.IsValueType(ins.Dst)) ins.Dst.Ptr   = context.GetPtr(ins.Dst);
        return ins;
    }

    #endregion
}