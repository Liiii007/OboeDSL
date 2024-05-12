using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OboeCompiler.Calc
{
    public enum InstructionType
    {
        None,
        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Store,
        Jump,
        Call,
        TrueAndJump,
        Larger,
        LargerEqual,
        Equal,
        NotEqual,
        Sin,
        Cos,
        Tan,
        Sqrt,
        End
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MathValue
    {
        [FieldOffset(0)] public float FloatValue;
        [FieldOffset(0)] public int   IntValue;
    }

    public struct MemPos
    {
        public enum MemType
        {
            None,
            Const,
            Reg,
            Var,
            Func,
            PC,
            External
        }

        public MemType Type;
        public int     Index;
        public IntPtr  Ptr;
    }

    //TODO:可变长参数
    //TODO:指针当MemPos，而不是索引
    public struct Instruction
    {
        public InstructionType Type;
        public MemPos          Src0;
        public MemPos          Src1;
        public MemPos          Dst;
    }

    public class OboeBackend
    {
        private int                     _variableCount = 0;
        private int                     _functionCount = 0;
        private int                     regUsage       = 0;
        public  int                     maxRegUsage    = 0;
        public  Dictionary<string, int> VariableIndex  = new Dictionary<string, int>();
        public  List<Instruction>       Instructions   = new List<Instruction>();
        public  List<float>             Constants      = new List<float>();

        public void AppendProgram(CalcParser.ProgContext program)
        {
            foreach (var line in program.children)
            {
                if (line is CalcParser.AssignContext assign)
                {
                    AppendAssign(assign);
                }

                if (line is CalcParser.IfstatementContext ifStatement)
                {
                    AppendIfStatement(ifStatement);
                }
            }
        }

        public void AppendAssign(CalcParser.AssignContext assign)
        {
            //TODO:支持+=/++等运算符
            var varDst = AllocIdIndex(assign.children[0] as CalcParser.IdContext);
            var srcPos = AppendExpr(assign.children[2] as CalcParser.MathexprContext);
            var store = new Instruction
            {
                Type = InstructionType.Store,
                Src0 = srcPos,
                Dst  = varDst
            };
            Instructions.Add(store);
        }

        public void AppendIfStatement(CalcParser.IfstatementContext ifStatement)
        {
            var boolValue = AppendBoolExpr(ifStatement.children[2] as CalcParser.BoolexprContext);

            var cmpInstrIndex = Instructions.Count;
            var cmpInstr = new Instruction
            {
                Type = InstructionType.TrueAndJump,
                Src0 = new MemPos
                {
                    Type  = MemPos.MemType.PC,
                    Index = cmpInstrIndex + 1,
                },
                Dst = boolValue
            };
            Instructions.Add(cmpInstr);

            AppendProgram(ifStatement.children[5] as CalcParser.ProgContext);

            Instructions.Add(new Instruction());
            int jumpToEndIndex = Instructions.Count - 1;

            cmpInstr.Src1 = new MemPos
            {
                Type  = MemPos.MemType.PC,
                Index = Instructions.Count,
            };
            Instructions[cmpInstrIndex] = cmpInstr;

            if (ifStatement.children[ifStatement.children.Count - 1] is CalcParser.ElsestatementContext elseStatement)
            {
                AppendProgram(elseStatement.children[2] as CalcParser.ProgContext);
            }

            var jumpToEnd = new Instruction
            {
                Type = InstructionType.Jump,
                Src0 = new MemPos
                {
                    Type  = MemPos.MemType.PC,
                    Index = Instructions.Count,
                }
            };
            Instructions[jumpToEndIndex] = jumpToEnd;
        }

        //TODO:适配||与&&
        private MemPos AppendBoolExpr(CalcParser.BoolexprContext expr)
        {
            #region Nested Handle

            var frontContext = expr.children[0];

            if (expr.children.Count == 1)
            {
                if (frontContext is CalcParser.IdContext idContext)
                {
                    return AppendId(idContext);
                }

                if (frontContext is CalcParser.ValueContext valueContext)
                {
                    return AppendConstant(valueContext);
                }
            }

            if (frontContext is CalcParser.MathfuncContext funcContext)
            {
                return AppendFunctionCall(funcContext);
            }

            if (frontContext.GetText() == "(" && expr.children[2].GetText() == ")")
            {
                return AppendExpr((CalcParser.MathexprContext)expr.children[1]);
            }

            var src0 = AppendExpr((CalcParser.MathexprContext)expr.children[0]);
            regUsage++;
            var src1 = AppendExpr((CalcParser.MathexprContext)expr.children[2]);

            var dst = new MemPos
            {
                Type  = MemPos.MemType.Reg,
                Index = regUsage - 1
            };
            maxRegUsage = Math.Max(maxRegUsage, regUsage + 1);
            regUsage--;

            #endregion

            var instruction = new Instruction
            {
                Src0 = src0,
                Src1 = src1,
                Dst  = dst
            };

            switch (expr.children[1].GetText())
            {
                case ">":
                    instruction.Type = InstructionType.Larger;
                    break;
                case ">=":
                    instruction.Type = InstructionType.LargerEqual;
                    break;
                case "==":
                    instruction.Type = InstructionType.Equal;
                    break;
                case "!=":
                    instruction.Type = InstructionType.NotEqual;
                    break;
                case "<":
                    (instruction.Src0, instruction.Src1) = (instruction.Src1, instruction.Src0);
                    instruction.Type                     = InstructionType.Larger;
                    break;
                case "<=":
                    (instruction.Src0, instruction.Src1) = (instruction.Src1, instruction.Src0);
                    instruction.Type                     = InstructionType.LargerEqual;
                    break;
                default:
                    throw new NotImplementedException("Unsupported calculate");
            }

            Instructions.Add(instruction);
            return dst;
        }

        private MemPos AllocIdIndex(CalcParser.IdContext assign)
        {
            var varName = assign.GetText();

            if (_linker.IdToIndex.TryGetValue(varName, out var index))
            {
                return new MemPos
                {
                    Type  = MemPos.MemType.External,
                    Index = index,
                };
            }
            else
            {
                if (!VariableIndex.TryGetValue(varName, out index))
                {
                    index                                       = _variableCount++;
                    VariableIndex[assign.children[0].GetText()] = index;
                }

                return new MemPos
                {
                    Type  = MemPos.MemType.Var,
                    Index = index,
                };
            }
        }

        private MemPos AppendExpr(CalcParser.MathexprContext expr)
        {
            #region Nested Handle

            var frontContext = expr.children[0];

            if (expr.children.Count == 1)
            {
                if (frontContext is CalcParser.IdContext idContext)
                {
                    return AppendId(idContext);
                }

                if (frontContext is CalcParser.ValueContext valueContext)
                {
                    return AppendConstant(valueContext);
                }
            }

            if (frontContext is CalcParser.MathfuncContext funcContext)
            {
                return AppendFunctionCall(funcContext);
            }

            if (frontContext.GetText() == "(" && expr.children[2].GetText() == ")")
            {
                return AppendExpr((CalcParser.MathexprContext)expr.children[1]);
            }

            var src0 = AppendExpr((CalcParser.MathexprContext)expr.children[0]);
            regUsage++;
            var src1 = AppendExpr((CalcParser.MathexprContext)expr.children[2]);

            var dst = new MemPos
            {
                Type  = MemPos.MemType.Reg,
                Index = regUsage - 1
            };
            maxRegUsage = Math.Max(maxRegUsage, regUsage + 1);
            regUsage--;

            #endregion

            var instruction = new Instruction
            {
                Src0 = src0,
                Src1 = src1,
                Dst  = dst
            };

            switch (expr.children[1].GetText())
            {
                case "+":
                    instruction.Type = InstructionType.Add;
                    Instructions.Add(instruction);
                    break;
                case "-":
                    instruction.Type = InstructionType.Sub;
                    Instructions.Add(instruction);
                    break;
                case "*":
                    instruction.Type = InstructionType.Mul;
                    Instructions.Add(instruction);
                    break;
                case "/":
                    instruction.Type = InstructionType.Div;
                    Instructions.Add(instruction);
                    break;
                default:
                    throw new NotImplementedException("Unsupported calculate");
            }

            return dst;
        }

        private MemPos AppendFunctionCall(CalcParser.MathfuncContext funcContext)
        {
            //TODO:变长函数
            MemPos callResult;
            if (funcContext.children[1].GetText() == "(" && funcContext.children[3].GetText() == ")")
            {
                var callArg0 = AppendExpr((CalcParser.MathexprContext)funcContext.children[2]);
                callResult = new MemPos()
                {
                    Type  = MemPos.MemType.Reg,
                    Index = regUsage
                };

                InstructionType functionType;

                switch (funcContext.children[0].GetText())
                {
                    case "sin":
                        functionType = InstructionType.Sin;
                        break;

                    case "cos":
                        functionType = InstructionType.Cos;
                        break;

                    case "tan":
                        functionType = InstructionType.Tan;
                        break;

                    case "sqrt":
                        functionType = InstructionType.Sqrt;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("Not function support");
                }

                Instructions.Add(new Instruction
                {
                    Type = functionType,
                    Src0 = callArg0,
                    Dst  = callResult
                });
            }
            else
            {
                throw new Exception();
            }

            return callResult;
        }

        private MemPos AppendConstant(CalcParser.ValueContext valueContext)
        {
            float value = float.Parse(valueContext.GetText());
            Constants.Add(value);

            return new MemPos
            {
                Type  = MemPos.MemType.Const,
                Index = Constants.Count - 1
            };
        }

        private MemPos AppendId(CalcParser.IdContext idContext)
        {
            var varName = idContext.GetText();

            if (_linker.IdToIndex.TryGetValue(varName, out var index))
            {
                return new MemPos
                {
                    Type  = MemPos.MemType.External,
                    Index = index,
                };
            }

            if (VariableIndex.TryGetValue(idContext.GetText(), out index))
            {
                return new MemPos
                {
                    Type  = MemPos.MemType.Var,
                    Index = index
                };
            }

            throw new Exception("Value not assign but use first!");
        }

        private OboeStructLinker _linker;

        public void SetLinker(OboeStructLinker linker)
        {
            _linker = linker;
        }
    }
}