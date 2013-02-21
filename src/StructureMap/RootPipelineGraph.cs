using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap.Graph;
using StructureMap.Interceptors;
using StructureMap.Pipeline;
using StructureMap.Query;
using StructureMap.Util;

namespace StructureMap
{
    public class RootPipelineGraph : IPipelineGraph
    {
        private readonly PluginGraph _pluginGraph;
        private readonly IObjectCache _transientCache;
        private readonly Cache<string, IPipelineGraph> _profiles; 

        public RootPipelineGraph(PluginGraph pluginGraph)
        {
            _pluginGraph = pluginGraph;
            _transientCache = new NulloTransientCache();
            _profiles =
                new Cache<string, IPipelineGraph>(
                    name => new ComplexPipelineGraph(this, _pluginGraph.Profile(name), new NulloTransientCache()));
        }

        public IObjectCache Singletons
        {
            get { return _pluginGraph.SingletonCache; }
        }

        public IObjectCache Transients
        {
            get { return _transientCache; }
        }

        public IPipelineGraph Root()
        {
            return this;
        }

        // TODO -- what if we cache these on the Instance itself?
        public InstanceInterceptor FindInterceptor(Type concreteType)
        {
            return _pluginGraph.InterceptorLibrary.FindInterceptor(concreteType);
        }

        public Instance GetDefault(Type pluginType)
        {
            return _pluginGraph.Families[pluginType].GetDefaultInstance();
        }

        public bool HasDefaultForPluginType(Type pluginType)
        {
            return _pluginGraph.HasDefaultForPluginType(pluginType);
        }

        public bool HasInstance(Type pluginType, string instanceKey)
        {
            return _pluginGraph.HasInstance(pluginType, instanceKey);
        }

        public void EachInstance(Action<Type, Instance> action)
        {
            _pluginGraph.EachInstance(action);
        }

        public IEnumerable<Instance> GetAllInstances()
        {
            return _pluginGraph.Families.SelectMany(x => x.Instances);
        }

        public IEnumerable<Instance> GetAllInstances(Type pluginType)
        {
            return _pluginGraph.AllInstances(pluginType);
        }

        public Instance FindInstance(Type pluginType, string name)
        {
            return _pluginGraph.FindInstance(pluginType, name);
        }

        public IPipelineGraph ForProfile(string profile)
        {
            return _profiles[profile];
        }

        public void ImportFrom(PluginGraph graph)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPluginTypeConfiguration> GetPluginTypes(IContainer container)
        {
            foreach (var family in _pluginGraph.Families)
            {
                if (family.IsGenericTemplate)
                {
                    yield return new GenericFamilyConfiguration(family);
                }
                else
                {
                    yield return new ClosedPluginTypeConfiguration(family, container, this);
                }
            }
        }

        public void Dispose()
        {
            _pluginGraph.EjectFamily(typeof(IContainer));
            _transientCache.DisposeAndClear();
        }

        public IPipelineGraph ToNestedGraph()
        {
            return new ComplexPipelineGraph(this, new PluginGraph(), new NestedContainerTransientObjectCache());
        }

        public IEnumerable<Type> AllPluginTypes()
        {
            return _pluginGraph.Families.Select(x => x.PluginType);
        }

        public IGraphEjector Ejector
        {
            get
            {
                return new GraphEjector(_pluginGraph);
            }
        }
    }
}