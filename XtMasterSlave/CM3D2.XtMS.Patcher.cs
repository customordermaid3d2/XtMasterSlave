using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CM3D2.XtMS.Patcher
{
    public static class Patcher
    {
        public static readonly string[] TargetAssemblyNames = { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition assembly)
        {
            Console.WriteLine("C*M3D2.XtMS.Patcher " + assembly);
            AssemblyDefinition ta = assembly;

            var prefix = "COM3D2";
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = "";
            if ( dir.EndsWith("sybaris", StringComparison.OrdinalIgnoreCase))
            {
                path = @"UnityInjector\";
            }
            else
            {
                path = @"..\Plugins\UnityInjector\";
            }
            if (Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location).StartsWith("cm3d2", StringComparison.OrdinalIgnoreCase))
            {
                prefix = "CM3D2";
            }

            // Helper使用
            try
            {
                AssemblyDefinition da = PatcherHelper.GetAssemblyDefinition(string.Format("{0}\\{1}.XtMasterSlave.Plugin.dll", path, prefix));
                TypeDefinition typedef = da.MainModule.GetType("CM3D2.XtMasterSlave.Plugin.Hook");

                PatcherHelper.SetHook(
                   PatcherHelper.HookType.PreCall,
                   ta, "TBody.AutoTwist",
                   da, "CM3D2.XtMasterSlave.Plugin.Hook.hookPreAutoTwist");

                // v5.1
                if (prefix == "COM3D2" && ta.MainModule.GetType("FullBodyIKCtrl") != null)
                {
                    PatcherHelper.SetHook(
                       PatcherHelper.HookType.PreCall,
                       ta, "FullBodyIKCtrl.SolverUpdate",
                       da, "CM3D2.XtMasterSlave.Plugin.Hook.hookPreSolverUpdate");
                }

                /*
                PatcherHelper.SetHook(
                   PatcherHelper.HookType.PostCall,
                   ta, "FullBodyIKCtrl.IKUpdate",
                   da, "CM3D2.XtMasterSlave.Plugin.Hook.hookIKPreLateUpdate");*/

                /*PatcherHelper.SetHook(
                   PatcherHelper.HookType.PostCall,
                   ta, "TBody.LateUpdate",
                   da, "CM3D2.XtMasterSlave.Plugin.Hook.hookBodyPreLateUpdate");*/
            }
            catch (Exception e)
            {
                Console.WriteLine("SetHook例外：" + e);
            }
        }
    }
}
