using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using MapsDirectlyToDatabaseTable.Injection;
using ReusableLibraryCode;

namespace CatalogueLibrary.Data.Pipelines
{
    /// <summary>
    /// Describes the flow of strongly typed objects (usually DataTables) from a source to a destination (e.g. extracting linked cohort data into a flat file ).  
    /// This entity is the serialized version of DataFlowPipelineEngine&lt;T&gt; (built by a DataFlowPipelineEngineFactory&lt;T&gt; ).
    /// 
    /// <para>It is the hanging off point of a sequence of steps e.g. 'clean strings', 'substitute column X for column Y by mapping values off of remote server B'.</para>
    /// 
    /// <para>The functionality of the class is like a microcosm of LoadMetadata (a sequence of predominately reflection driven operations) but it happens in memory 
    /// (rather than in the RAW=>STAGING=>LIVE databases).</para>
    /// 
    /// <para>Any time data flows from one location to another there is usually a pipeline involved (e.g. read from a flat file and bulk insert into a database), it 
    /// may be an empty pipeline but the fact that it is there allows for advanced/freaky user requirements such as:</para>
    ///
    /// <para>"Can we count all dates to the first Monday of the week on all extracts we do from now on? - it's a requirement of our new Data Governance Officer"</para>
    /// 
    /// <para>A Pipeline can be missing either/both a source and destination.  This means that the pipeline can only be used in a situation where the context forces
    /// a particular source/destination (for example if the user is trying to bulk insert a CSV file then the Destination might be a fixed instance of DataTableUploadDestination
    /// initialized with a specific server/database that the user had picked on a user interface).</para>
    /// 
    /// <para>Remember that Pipeline is the serialization, pipelines are used all over the place in RDMP software under different contexts (caching, data extraction etc)
    /// and sometimes we even create DataFlowPipelineEngine on the fly without even having a Pipeline serialization to create it from.</para>
    /// </summary>
    public class Pipeline : VersionedDatabaseEntity, IPipeline,IHasDependencies, IInjectKnown<IPipelineComponent[]>
    {
        #region Database Properties

        private string _name;
        private string _description;
        private int? _destinationPipelineComponentID;
        private int? _sourcePipelineComponentID;

        public string Name
        {
            get { return _name; }
            set { SetField(ref  _name, value); }
        }

        public string Description
        {
            get { return _description; }
            set { SetField(ref  _description, value); }
        }

        public int? DestinationPipelineComponent_ID
        {
            get { return _destinationPipelineComponentID; }
            set { SetField(ref  _destinationPipelineComponentID, value); }
        }

        public int? SourcePipelineComponent_ID
        {
            get { return _sourcePipelineComponentID; }
            set { SetField(ref  _sourcePipelineComponentID, value); }
        }

        #endregion

        #region Relationships
        [NoMappingToDatabase]
        public IList<IPipelineComponent> PipelineComponents { get
        {
            return _knownPipelineComponents.Value;
        }}

        [NoMappingToDatabase]
        public IPipelineComponent Destination {
            get
            {
                return DestinationPipelineComponent_ID == null
                    ? null
                    :_knownPipelineComponents.Value.Single(c=>c.ID == DestinationPipelineComponent_ID.Value);
            }
        }

        [NoMappingToDatabase]
        public IPipelineComponent Source
        {
            get
            {
                return SourcePipelineComponent_ID == null
                    ? null
                    :_knownPipelineComponents.Value.Single(c=>c.ID == SourcePipelineComponent_ID.Value);
            }
        }

        #endregion

        public Pipeline(ICatalogueRepository repository, string name = null)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>
            {
                {"Name", name ?? "NewPipeline " + Guid.NewGuid()}
            });
            
            ClearAllInjections();
        }

        internal Pipeline(ICatalogueRepository repository, DbDataReader r)
            : base(repository, r)
        {
            Name = r["Name"].ToString();

            object o=  r["DestinationPipelineComponent_ID"];
            if (o == DBNull.Value)
                DestinationPipelineComponent_ID = null;
            else
                DestinationPipelineComponent_ID = Convert.ToInt32(o);

            o = r["SourcePipelineComponent_ID"];
            if (o == DBNull.Value)
                SourcePipelineComponent_ID = null;
            else
                SourcePipelineComponent_ID = Convert.ToInt32(o);

            Description = r["Description"] as string;

            ClearAllInjections();
        }
        
        public override string ToString()
        {
            return Name;
        }

        public Pipeline Clone()
        {
            var clonePipe = new Pipeline((ICatalogueRepository)Repository, Name + "(Clone)");
            clonePipe.Description = Description;

            var originalSource = Source;
            if (originalSource != null)
            {
                var cloneSource = originalSource.Clone(clonePipe);
                clonePipe.SourcePipelineComponent_ID = cloneSource.ID;
            }
            var originalDestination = Destination;
            if (originalDestination != null)
            {
                var cloneDestination = originalDestination.Clone(clonePipe);
                clonePipe.DestinationPipelineComponent_ID = cloneDestination.ID;
            }

            clonePipe.SaveToDatabase();

            foreach (IPipelineComponent component in PipelineComponents)
            {
                //if the component is one of the ones we already cloned
                if (Equals(originalSource, component) || Equals(originalDestination, component))
                    continue;

                component.Clone(clonePipe);
            }


            return clonePipe;
        }

        public IHasDependencies[] GetObjectsThisDependsOn()
        {
            return new IHasDependencies[0];
        }

        public IHasDependencies[] GetObjectsDependingOnThis()
        {
            return PipelineComponents.Cast<IHasDependencies>().ToArray();
        }

        private Lazy<IList<IPipelineComponent>> _knownPipelineComponents;

        public void InjectKnown(IPipelineComponent[] instance)
        {
            _knownPipelineComponents = new Lazy<IList<IPipelineComponent>>(()=>instance.OrderBy(p=>p.Order).ToList());
        }

        public void ClearAllInjections()
        {
            _knownPipelineComponents = new Lazy<IList<IPipelineComponent>>(FetchPipelineComponents);
        }

        private IList<IPipelineComponent> FetchPipelineComponents()
        {
            return Repository.GetAllObjectsWithParent<PipelineComponent>(this)
                .Cast<IPipelineComponent>()
                .OrderBy(p => p.Order)
                .ToList();
        }
    }
}
