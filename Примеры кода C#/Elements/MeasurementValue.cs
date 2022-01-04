using System;
using System.Collections.Generic;
using Aggregator.Services;

namespace Aggregator.Elements
{
    using CIM = Monitel.Mal.Context.CIM16;

    public sealed class MeasurementValue : IElement
    {
        private List<AggregatedAnalogValue> dependencies;

        private void LoadHISPartition()
        {
            string name = nameof(CIM.MeasurementValue.HISPartition);
            if (HISPartition == null)
                throw new ElementLoadException($"{name} is not defined");
            HISPartition.Load(name);
            StepIntervalInSeconds = HISPartition.StepIntervalInSeconds;
        }

        public MeasurementValue(Guid uid, long id, string name)
        {
            Uid = uid;
            Id = id;
            Name = name;
        }

        public MeasurementValue(Core core, long id, HISPartition hisPartition)
            : this(new Guid((int)id, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), id, $"MeasurementValue{id}")
        {
            HISPartition = hisPartition;
            core.Elements.Add(id, this);
            core.MeasurementValues.Add(this);
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        public void Load(ILogger logger, AggregatedAnalogValue obj)
        {
            if (!IsLoaded)
            {
                IsLoaded = true;
                try
                {
                    LoadHISPartition();
                    IsValid = true;
                }
                catch (ElementLoadException ex)
                {
                    IsValid = false;
                    logger.Warning($"ModelBuilder> Error load {nameof(CIM.MeasurementValue)} {Uid}: {ex.Message}");
                }
            }

            if (IsValid && obj != null && StepIntervalInSeconds != 0)
            {
                if (dependencies == null)
                    dependencies = new List<AggregatedAnalogValue>(1) { obj };
                else if (!dependencies.Contains(obj))
                    dependencies.Add(obj);
            }
        }

        public void RemoveDependency(AggregatedAnalogValue obj)
        {
            if (dependencies != null)
                dependencies.Remove(obj);
        }

        public void OnReadSubscriptionValue(ValueDataItem item)
        {
            if (dependencies == null)
                return;
            foreach (var p in dependencies)
                p.OnReadSubscriptionMeasurementValue(item);
        }

        public bool HasDependencies => dependencies != null;

        public IEnumerable<AggregatedAnalogValue> GetDependencies()
        {
            return dependencies;
        }

        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public string Name { get; private set; }
        public HISPartition HISPartition { get; set; }
        public int StepIntervalInSeconds { get; private set; }
        public bool IsByChange => StepIntervalInSeconds == 0;
        public bool IsFixedStep => StepIntervalInSeconds != 0;
        public bool IsLoaded { get; private set; } = false;
        public bool IsValid { get; private set; } = false;
    }
}
