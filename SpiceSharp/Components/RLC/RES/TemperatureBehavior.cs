﻿using System;
using SpiceSharp.Diagnostics;
using SpiceSharp.Behaviors;
using SpiceSharp.Simulations;

namespace SpiceSharp.Components.ResistorBehaviors
{
    /// <summary>
    /// Temperature behavior for a <see cref="Resistor"/>
    /// </summary>
    public class TemperatureBehavior : Behaviors.TemperatureBehavior
    {
        /// <summary>
        /// Necessary parameters
        /// </summary>
        ModelBaseParameters mbp;
        BaseParameters bp;

        /// <summary>
        /// Get the default conductance for this model
        /// </summary>
        public double Conductance { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        public TemperatureBehavior(Identifier name) : base(name) { }

        /// <summary>
        /// Setup the behavior
        /// </summary>
        /// <param name="provider"></param>
        public override void Setup(SetupDataProvider provider)
        {
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

            // Get parameters
            bp = provider.GetParameterSet<BaseParameters>(0);
            if (!bp.Resistance.Given)
                mbp = provider.GetParameterSet<ModelBaseParameters>(1);
        }
        
        /// <summary>
        /// Execute behavior
        /// </summary>
        /// <param name="sim">Base simulation</param>
        public override void Temperature(BaseSimulation sim)
        {
			if (sim == null)
				throw new ArgumentNullException(nameof(sim));

            double factor;
            double difference;
            double RESresist = bp.Resistance;

            // Default Value Processing for Resistor Instance
            if (!bp.Temperature.Given)
                bp.Temperature.Value = sim.State.Temperature;
            if (!bp.Width.Given)
                bp.Width.Value = mbp?.DefWidth ?? 0.0;

            if (mbp != null)
            {
                if (mbp.SheetResistance.Given && (mbp.SheetResistance != 0) && (bp.Length != 0))
                    RESresist = mbp.SheetResistance * (bp.Length - mbp.Narrow) / (bp.Width - mbp.Narrow);
                else
                {
                    CircuitWarning.Warning(this, $"{Name}: resistance=0, set to 1000");
                    RESresist = 1000;
                }

                difference = bp.Temperature - mbp.NominalTemperature;
                factor = 1.0 + (mbp.TemperatureCoefficient1) * difference + (mbp.TemperatureCoefficient2) * difference * difference;
            }
            else
            {
                difference = bp.Temperature - 300.15;
                factor = 1.0;
            }

            Conductance = 1.0 / (RESresist * factor);
        }
    }
}
