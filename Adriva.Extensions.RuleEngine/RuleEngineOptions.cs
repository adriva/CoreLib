using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Adriva.Extensions.RuleEngine
{
    public class RuleEngineOptions
    {
        internal List<Assembly> Assemblies = new List<Assembly>();

        public bool UseCache { get; set; }

        public void AddAssembly(Assembly assembly)
        {
            if (null == assembly) return;
            if (this.Assemblies.Any(a => a.GetName() == assembly.GetName())) return;
            this.Assemblies.Add(assembly);
        }
    }
}
