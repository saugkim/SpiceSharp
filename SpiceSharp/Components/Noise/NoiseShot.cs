﻿using System;
using SpiceSharp.Simulations;

namespace SpiceSharp.Components.NoiseSources
{
    /// <summary>
    /// Shotnoise generator
    /// </summary>
    public class NoiseShot : NoiseGenerator
    {
        /// <summary>
        /// Gets or sets the gain of the shot noise
        /// The noise will be 2 * q * Gain
        /// </summary>
        public double Current { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the noise source</param>
        public NoiseShot(string name, int node1, int node2) : base(name, node1, node2) { }

        /// <summary>
        /// Set the parameters of the shot noise
        /// </summary>
        /// <param name="coefficients">Values</param>
        public override void SetCoefficients(params double[] coefficients)
        {
            if (coefficients == null)
                throw new ArgumentNullException(nameof(coefficients));
            Current = coefficients[0];
        }

        /// <summary>
        /// Calculate the noise contribution
        /// </summary>
        /// <param name="simulation">Noise simulation</param>
        /// <returns></returns>
        protected override double CalculateNoise(Noise simulation)
        {
            if (simulation == null)
                throw new ArgumentNullException(nameof(simulation));

            var state = simulation.State;
            var sol = state.Solution;
            var isol = state.iSolution;
            var rval = sol[Nodes[0]] - sol[Nodes[1]];
            var ival = isol[Nodes[0]] - isol[Nodes[1]];
            double gain = rval * rval + ival * ival;
            return 2.0 * Circuit.Charge * Math.Abs(Current) * gain;
        }
    }
}
