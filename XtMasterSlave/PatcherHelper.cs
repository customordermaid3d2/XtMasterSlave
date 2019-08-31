//PostCallバグ修正：2017/05/27＠nn 
//ExPreCallフックタイプを追加：2017/08/20＠nn（戻りがvoidのメソッド限定ですが実行中にPreJump/PreCallを切換え可）
//ExPostCallRetフックタイプを追加：2017/09＠nn
//PostCall系バグ修正＋Lastフックタイプ追加：2017/01/31＠nn

using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class PatcherHelper
{
    public delegate void InsertInstDelegate(Instruction newInst);

    public static AssemblyDefinition GetAssemblyDefinition(string assemblyName)
    {
        var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var filename = Path.Combine(directoryName, assemblyName);
        AssemblyDefinition ad = AssemblyDefinition.ReadAssembly(filename);
        if (ad == null)
        {
            throw new Exception(string.Format("{0} not found", assemblyName));
        }
        return ad;
    }

    public static AssemblyDefinition GetAssemblyDefinition2(string assemblyName)
    {

        Console.WriteLine("GetAssemblyDefinition2 : " + assemblyName);
        var directoryName = Path.GetDirectoryName((Assembly.GetExecutingAssembly().Location));
        Console.WriteLine("GetAssemblyDefinition2 : " + directoryName);
        var filename = Path.Combine(directoryName, @"..\..\CM3D2x64_Data\Managed");
        if (!Directory.Exists(filename))
            filename = Path.Combine(directoryName, @"..\..\CM3D2x86_Data\Managed");
        filename = Path.Combine(filename, assemblyName);

        Console.WriteLine("GetAssemblyDefinition2 : " + filename);
        AssemblyDefinition ad = AssemblyDefinition.ReadAssembly(filename);
        if (ad == null)
        {
            throw new Exception(string.Format("{0} not found", assemblyName));
        }
        return ad;
    }

    public static MethodDefinition GetMethod(TypeDefinition type, string methodName)
    {
        return type.Methods.FirstOrDefault(m => m.Name == methodName);
    }

    public static MethodDefinition GetMethod(TypeDefinition type, string methodName, params string[] args)
    {
        if (args == null)
        {
            return GetMethod(type, methodName);
        }
        for (int i = 0; i < type.Methods.Count; i++)
        {
            MethodDefinition m = type.Methods[i];
            if (m.Name == methodName && m.Parameters.Count == args.Length)
            {
                bool b = true;
                for (int j = 0; j < args.Length; j++)
                {
                    if (m.Parameters[j].ParameterType.FullName != args[j])
                    {
                        b = false;
                        break;
                    }
                }
                if (b)
                {
                    return m;
                }
            }
        }
        return null;
    }

    public static void SetHook(
        HookType hookType,
        AssemblyDefinition targetAssembly, string targetName,
        AssemblyDefinition calleeAssembly, string calleeName)
    {
        int i0 = targetName.LastIndexOf('.');
        if (i0 < 0)
        {
            throw new Exception(string.Format("SetHook - Error : Bad Name ({0})", targetName));
        }
        string targetTypeName = targetName.Substring(0, i0);
        string targetMethodName = targetName.Substring(i0 + 1);

        int i1 = calleeName.LastIndexOf('.');
        if (i1 < 0)
        {
            throw new Exception(string.Format("SetHook - Error : Bad Name ({0})", calleeName));
        }
        string calleeTypeName = calleeName.Substring(0, i1);
        string calleeMethodName = calleeName.Substring(i1 + 1);

        SetHook(hookType,
            targetAssembly, targetTypeName, targetMethodName,
            calleeAssembly, calleeTypeName, calleeMethodName);
    }

    public static void SetHook(
        HookType hookType,
        AssemblyDefinition targetAssembly, string targetName, string[] targetArgTypes,
        AssemblyDefinition calleeAssembly, string calleeName, string[] calleeArgTypes)
    {
        int i0 = targetName.LastIndexOf('.');
        if (i0 < 0)
        {
            throw new Exception(string.Format("SetHook - Error : Bad Name ({0})", targetName));
        }
        string targetTypeName = targetName.Substring(0, i0);
        string targetMethodName = targetName.Substring(i0 + 1);

        int i1 = calleeName.LastIndexOf('.');
        if (i1 < 0)
        {
            throw new Exception(string.Format("SetHook - Error : Bad Name ({0})", calleeName));
        }
        string calleeTypeName = calleeName.Substring(0, i1);
        string calleeMethodName = calleeName.Substring(i1 + 1);

        SetHook(hookType,
                targetAssembly, targetTypeName, targetMethodName, targetArgTypes,
                calleeAssembly, calleeTypeName, calleeMethodName, calleeArgTypes);
    }

    public static void SetHook(
        HookType hookType,
        AssemblyDefinition targetAssembly, string targetTypeName, string targetMethodName,
        AssemblyDefinition calleeAssembly, string calleeTypeName, string calleeMethodName)
    {
#if DEBUG
        Console.WriteLine("SetHook - {0}/{1}|{2} -> {3}/{4}|{5}", targetAssembly.Name.Name, targetTypeName, targetMethodName, calleeAssembly.Name.Name, calleeTypeName, calleeMethodName);
#endif
        TypeDefinition calleeTypeDefinition = calleeAssembly.MainModule.GetType(calleeTypeName);
        if (calleeTypeDefinition == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1} is not found", calleeAssembly.Name, calleeTypeName));
        }

        MethodDefinition calleeMethod = GetMethod(calleeTypeDefinition, calleeMethodName);
        if (calleeMethod == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1}.{2} is not found", calleeAssembly.Name, calleeTypeName, calleeMethodName));
        }

        TypeDefinition targetTypeDefinition = targetAssembly.MainModule.GetType(targetTypeName);
        if (targetTypeDefinition == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1} is not found", targetAssembly.Name, targetTypeName));
        }

        MethodDefinition targetMethod = GetMethod(targetTypeDefinition, targetMethodName);
        if (targetMethod == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1}.{2} is not found", targetAssembly.Name, targetTypeName, targetMethodName));
        }
        HookMethod(hookType, targetAssembly.MainModule, targetMethod, calleeMethod);
    }

    public static void SetHook(
        HookType hookType,
        AssemblyDefinition targetAssembly, string targetTypeName, string targetMethodName, string[] targetArgTypes,
        AssemblyDefinition calleeAssembly, string calleeTypeName, string calleeMethodName, string[] calleeArgTypes)
    {
#if DEBUG
        Console.WriteLine("SetHook - {0}/{1}|{2}({3}) -> {4}/{5}|{6}({7})",
                          targetAssembly.Name.Name, targetTypeName, targetMethodName, string.Join(",", targetArgTypes),
                          calleeAssembly.Name.Name, calleeTypeName, calleeMethodName, string.Join(",", calleeArgTypes));
#endif
        TypeDefinition calleeTypeDefinition = calleeAssembly.MainModule.GetType(calleeTypeName);
        if (calleeTypeDefinition == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1} is not found", calleeAssembly.Name, calleeTypeName));
        }

        MethodDefinition calleeMethod = GetMethod(calleeTypeDefinition, calleeMethodName, calleeArgTypes);
        if (calleeMethod == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1}.{2} is not found", calleeAssembly.Name, calleeTypeName, calleeMethodName));
        }

        TypeDefinition targetTypeDefinition = targetAssembly.MainModule.GetType(targetTypeName);
        if (targetTypeDefinition == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1} is not found", targetAssembly.Name, targetTypeName));
        }

        MethodDefinition targetMethod = GetMethod(targetTypeDefinition, targetMethodName, targetArgTypes);
        if (targetMethod == null)
        {
            throw new Exception(string.Format("Error ({0}) : {1}.{2} is not found", targetAssembly.Name, targetTypeName, targetMethodName));
        }
        HookMethod(hookType, targetAssembly.MainModule, targetMethod, calleeMethod);
    }

    public static void HookMethod(
        HookType hookType,
        ModuleDefinition targetModule, MethodDefinition targetMethod,
        MethodDefinition calleeMethod)
    {
        Console.WriteLine("HookMethod " + hookType + "/" + targetModule + "/" + targetMethod + "/" + calleeMethod);
        Console.WriteLine("ImportReference" + targetModule.ImportReference(calleeMethod));
        Console.WriteLine("***");

        ILProcessor l = targetMethod.Body.GetILProcessor();
        Instruction instInsertPoint = targetMethod.Body.Instructions.First();

        if (hookType == HookType.PostCallLast || hookType == HookType.PostCallLastReplace)
        {
            //instInsertPoint = targetMethod.Body.Instructions.Last();
            //一番最後のretだけを検索してパッチする
            Instruction ret = targetMethod.Body.Instructions.Last(i => i.OpCode == OpCodes.Ret);
            int index = targetMethod.Body.Instructions.IndexOf(ret);

            var il = targetMethod.Body.GetILProcessor();

            InsertInstDelegate o2 = newInst =>
            {
                l.InsertBefore(ret, newInst);
            };
            if (hookType == HookType.PostCallLastReplace)
            {
                // 新しいretを追加
                var ret2 = il.Create(OpCodes.Ret);
                il.InsertAfter(ret, ret2);

                // 元のretを置換
                il.ReplaceInstruction(ret, il.Create(OpCodes.Nop));
                ret = ret2;
            }

            // 引数をセット
            int n2 = targetMethod.Parameters.Count + (targetMethod.IsStatic ? 0 : 1);
            for (int i = 0; i < n2; i++)
            {
                if (i == 0)
                {
                    o2(l.Create(OpCodes.Ldarg_0));
                }
                else
                {
                    // ref 参照にしたい場合は OpCodes.Ldarga にすること
                    o2(l.Create(OpCodes.Ldarg, i));
                }
            }
            
            // Hookメソッド呼び出し
            il.InsertBefore(ret, il.Create(OpCodes.Call, targetModule.ImportReference(calleeMethod)));

            //Instruction call = targetMethod.Body.Instructions.Last(i => i.OpCode == OpCodes.Ldarg_0);

            // デバッグ用出力
            for (int i = index; i < targetMethod.Body.Instructions.Count; i++)
            {
                Instruction testx = targetMethod.Body.Instructions[i];
                Console.WriteLine("*** il.Code({0}): " + testx, i);
            }
            return;
        }

        if (hookType == HookType.PostCall || hookType == HookType.PostCallRet || hookType == HookType.ExPostCallRet)
        {
            //instInsertPoint = targetMethod.Body.Instructions.Last();
            //全てのretを検索してパッチする
            targetMethod.Body.Instructions
                .Where(i => i.OpCode == OpCodes.Ret)
                .ToList()
                .ForEach(end =>
                {
                    Console.WriteLine("*** il.InsertAfter:" + end);

                    //ret位置にジャンプしてくる処理への対策としてret位置をずらす
                    int index = targetMethod.Body.Instructions.IndexOf(end);
                    Console.WriteLine("*** index:" + index + "/" + (targetMethod.Body.Instructions.Count() - 1));
                    l.InsertAfter(end, l.Create(OpCodes.Ret));

                    l.ReplaceInstruction(end, l.Create(OpCodes.Nop));  // bug fix
                    Console.WriteLine("*** Replace({0}): Ret -> " + /*OpCodes.Nop*/ targetMethod.Body.Instructions[index], index);

                    Instruction next = targetMethod.Body.Instructions[index + 1];
                    Console.WriteLine("*** il.InsertBefore({0}):" + next, index + 1);
                    InsertInstDelegate o2 = newInst =>
                    {
                        l.InsertBefore(next, newInst);
                    };

                    // todo 2があるとは限らないので、良い方法を探す（ExPostCallRetなら一応汎用使用可）
                    int tmpLoc2 = 2;
                    if (hookType == HookType.PostCallRet)
                    {
                        // 戻り値をテンポラリにコピー
                        o2(l.Create(OpCodes.Dup));           // 最後の ret 用にコピーを作る
                        o2(l.Create(OpCodes.Stloc, tmpLoc2));
                    }
                    if (hookType == HookType.ExPostCallRet)
                    {
                        // 戻り値をテンポラリにコピーしない
                        // 元の戻り値は第一引数に入る
                    }
                    int iTest = 0;
                    int n2 = targetMethod.Parameters.Count + (targetMethod.IsStatic ? 0 : 1);
                    for (int i = 0; i < n2; i++)
                    {
                        if (i == 0)
                        {
                            o2(l.Create(OpCodes.Ldarg_0));
                        }
                        else
                        {
                            // ref 参照にしたい場合は OpCodes.Ldarga にすること
                            o2(l.Create(OpCodes.Ldarg, i));
                        }
                        iTest++;
                    }
                    if (hookType == HookType.PostCallRet)
                    {
                        // 戻り値をテンポラリからスタックへコピー
                        o2(l.Create(OpCodes.Ldloc, tmpLoc2));
                    }
                    o2(l.Create(OpCodes.Call, targetModule.ImportReference(calleeMethod)));

                    for (int i = 0; i <= iTest; i++)
                    {
                        Instruction testx = targetMethod.Body.Instructions[index + 1 + i];
                        Console.WriteLine("*** il.InsertBefore({0}):" + testx, index + 1 + i);

                    }

                });
            return;
        }

        Console.WriteLine("*** il.InsertBefore:" + instInsertPoint);
        InsertInstDelegate o = newInst =>
        {
            l.InsertBefore(instInsertPoint, newInst);
        };

        /*ここにはこない
        // todo 2があるとは限らないので、良い方法を探す
        int tmpLoc = 2;
        if (hookType == HookType.PostCallRet)
        {
            // 戻り値をテンポラリにコピー
            o(l.Create(OpCodes.Dup));           // 最後の ret 用にコピーを作る
            o(l.Create(OpCodes.Stloc, tmpLoc));
        }*/

        int n = targetMethod.Parameters.Count + (targetMethod.IsStatic ? 0 : 1);
        for (int i = 0; i < n; i++)
        {
            if (i == 0)
            {
                o(l.Create(OpCodes.Ldarg_0));
            }
            else
            {
                // ref 参照にしたい場合は OpCodes.Ldarga にすること
                o(l.Create(OpCodes.Ldarg, i));
            }
        }
        /*ここにはこない
        if (hookType == HookType.PostCallRet)
        {
            // 戻り値をテンポラリからスタックへコピー
            o(l.Create(OpCodes.Ldloc, tmpLoc));
        }*/
        o(l.Create(OpCodes.Call, targetModule.ImportReference(calleeMethod)));

        // PreJumpの場合は元の処理を行わないように、そのままRetする
        if (hookType == HookType.PreJump)
        {
            o(l.Create(OpCodes.Ret));
        }
        // ExPreCallの場合は戻り値がFalseなら、元の処理を行わないように、そのままRetする

        if (hookType == HookType.ExPreCall)
        {
            Console.WriteLine("*** HookType.ExPreCall:" + instInsertPoint.Previous + "/" + String.Format("{0:X}", instInsertPoint.Previous.GetHashCode()));
            //Console.WriteLine(l.Create(OpCodes.Brfalse, instInsertPoint));
            //Console.WriteLine(l.Create(OpCodes.Brfalse_S, instInsertPoint));
            o(l.Create(OpCodes.Brfalse_S, instInsertPoint));

            Console.WriteLine("*** HookType.ExPreCall:" + instInsertPoint.Previous + "/" + String.Format("{0:X}", instInsertPoint.Previous.GetHashCode()) + " → " + instInsertPoint + "/" + String.Format("{0:X}", instInsertPoint.GetHashCode()));
            o(l.Create(OpCodes.Ret));

            Console.WriteLine("*** HookType.ExPreCall:" + instInsertPoint.Previous + "/" + String.Format("{0:X}", instInsertPoint.Previous.GetHashCode()));
            Console.WriteLine("*** HookType.ExPreCall:" + instInsertPoint + "/" + String.Format("{0:X}", instInsertPoint.GetHashCode()));
        }
    }

    public enum HookType
    {
        PreJump,        // 元メソッドの先頭で、置き換え先メソッドへジャンプし、元のメソッドの処理は一切行わずに終了する
        PreCall,        // 元メソッドの先頭で、置き換え先メソッドをコールし、その後通常通り元のメソッドの処理を行う
        PostCall,       // 元メソッドの処理が完了したあと、リターンする直前で置き換え先メソッドを呼び出す
        PostCallRet,    // 元メソッドの処理が完了したあと、リターンする直前で置き換え先メソッドを呼び出す。置き換えメソッドの最後の引数には戻り値を入れる
        ExPreCall,        // 元メソッドの先頭で、置き換え先メソッドをコールし、その戻り値がfalseなら通常通り元のメソッドの処理を行う(※戻り値がvoidのメソッド限定)
        ExPostCallRet,    // 元メソッドの処理が完了したあと、リターンする直前で置き換え先メソッドを呼び出す。置き換えメソッドの最初の引数には戻り値を入れる。戻り値は置き換えメソッドの物に置き換える
        PostCallLast,       // 元メソッドの処理が完了したあと、一番最後のリターン直前で置き換え先メソッドを呼び出す（元のretにジャンプしてくる命令はそのまま）
        PostCallLastReplace,       // 元メソッドの処理が完了したあと、一番最後のリターンを置き換え先メソッドの呼び出しに置換し、その後リターンする 
    }

    // ILProcessor.Replace bug with branch targets #182
    public static void ReplaceInstruction(this ILProcessor processor, Instruction from, Instruction to)
    {
        foreach (var item in processor.Body.Instructions)
        {
            var operInstruction = item.Operand as Instruction;
            if (operInstruction != null && operInstruction == from)
            {
                item.Operand = to;
            }
        }

        processor.Replace(from, to);
    }
}
