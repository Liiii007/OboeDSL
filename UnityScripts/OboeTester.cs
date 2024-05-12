using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Antlr4.Runtime;
using OboeCompiler;
using OboeCompiler.Calc;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public unsafe class OboeTester : MonoBehaviour
{
    private static CalcParser.ProgContext GetRootProgramAST(string input)
    {
        AntlrInputStream       inputStream = new AntlrInputStream(input);
        CalcLexer              lexer       = new CalcLexer(inputStream);
        CommonTokenStream      tokens      = new CommonTokenStream(lexer);
        CalcParser             parser      = new CalcParser(tokens);
        CalcParser.ProgContext root        = parser.prog();
        return root;
    }

    private NativeArray<Instruction> instrs_native;

    OboeStructLinker linker = new OboeStructLinker();

    private void Start()
    {
        string input =
            "$bind.a=2;if($bind.a==2){$bind.a=sin(cos(1)+sin(2));}else{$bind.b=sin(cos(1)+sin(2)+cos(3))+sin(4);}";
        var root = GetRootProgramAST(input);

        OboeBackend    compiler = new OboeBackend();
        Instruction[]  instrs;
        ExecuteContext context;

        BindTest.AddId(linker);
        compiler.SetLinker(linker);
        compiler.AppendProgram(root);
        instrs        = compiler.Instructions.ToArray();
        instrs_native = new NativeArray<Instruction>(instrs.Length, Allocator.Persistent);
        for (int i = 0; i < instrs.Length; i++)
        {
            instrs_native[i] = instrs[i];
        }

        context = ExecuteContext.GetExecuteContext(compiler);
        OboeBurstVM.LinkContext(instrs_native, context);
    }

    private BindTest test;

    private void Update()
    {
        fixed (BindTest* ptr = &test)
        {
            BindTest.BindValue(ptr, linker);

            Profiler.BeginSample("Run script");
            JobHandle jobHandle = new JobHandle();
            for (int i = 0; i < 10000; i++)
            {
                //每次执行前都重新绑定一次外部变量，内部变量不需要重新绑定
                Profiler.BeginSample("Link external");
                OboeBurstVM.LinkExternal(instrs_native, linker);
                Profiler.EndSample();

                new OboeBurstVM { Instructions = instrs_native }.Run();
            }

            jobHandle.Complete();
            Profiler.EndSample();
        }
    }
}