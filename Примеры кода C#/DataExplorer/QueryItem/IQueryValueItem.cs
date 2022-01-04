using System;

namespace Monitel.SCADA.Tools.DataExplorer.Models
{
    public interface IQueryValueItem
    {
        long Id { get; }
        Guid Uid { get; }
        string Name { get; }
        string DisplayName { get; }
        string SpecificName { get; }
        string Path { get; }
        object Value { get; }
        uint Flags { get; }
        DateTime? Tstamp { get; }
        DateTime? QueryTime { get; set; }
        DateTime? Tstamp2 { get; }
        DateTime? TstampLocal { get; }
        DateTime? Tstamp2Local { get; }
        double TimeDif { get; }        
        string MetaTypeName { get; }
        IQueryValueItem Self { get; }
        bool? InFilter { get; set; }
        int? AddrInt1 { get; }
        int? AddrInt2 { get; }
        string AddrStr1 { get; }
    }

    public interface IQueryDiscreteValueItem : IQueryValueItem
    {
        long DiscreteValue { get; }       
    }

    public interface IQueryAnalogValueItem : IQueryValueItem
    {      
        double AnalogValue { get; }
    }

    public interface IQueryStringValueItem : IQueryValueItem
    {
        string StringValue { get; }
    }
}
