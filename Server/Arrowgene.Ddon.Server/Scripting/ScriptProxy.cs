using Arrowgene.Ddon.GameServer.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.Server.Scripting
{
    public class ScriptProxy : MarshalByRefObject
    {
        private AssemblyLoadContext Context { get; set; }
        private Assembly ScriptAssembly { get; set; }
        private string DllPath { get; set; }

        public ScriptProxy(string dllPath)
        {
            DllPath = dllPath;
        }

        public void LoadAssembly()
        {
            Context = new AssemblyLoadContext($"ScriptContext_{Guid.NewGuid()}", isCollectible: true);
            using (var stream = File.OpenRead(DllPath))
            {
                ScriptAssembly = Context.LoadFromStream(stream);
            }
        }

        public void UnloadAssembly()
        {
            if (Context == null)
            {
                return;
            }

            Context.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public async void Execute(ScriptModule module, string scriptPath)
        {
            var type = ScriptAssembly.GetType("Submission#0");
            var factory = type.GetMethod("<Factory>");
            var submissionArray = new object[2];
            Task<object> task = (Task<object>)factory.Invoke(null, [submissionArray]);
            await task;

            Dictionary<string, object> variables = [];
            foreach (var member in type.GetMembers())
            {
                if (member is FieldInfo fieldInfo)
                {
                    object value = fieldInfo.GetValue(submissionArray[1]);
                    if (value is not null)
                    {
                        variables.Add(fieldInfo.Name, value);
                    }
                }
            }

            if (!module.EvaluateResult(scriptPath, task.Result, variables))
            {
                throw new Exception("Failed to evaluate the result of executing a stored DLL.");
            }
        }
    }
}
