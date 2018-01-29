﻿using System;
using SpiceSharp.Sparse;
using SpiceSharp.IntegrationMethods;
using SpiceSharp.Simulations;
using SpiceSharp.Behaviors;

namespace SpiceSharp.Components.MosfetBehaviors.Level2
{
    /// <summary>
    /// Transient behavior for a <see cref="Mosfet2"/>
    /// </summary>
    public class TransientBehavior : Behaviors.TransientBehavior, IConnectedBehavior
    {
        /// <summary>
        /// Necessary behaviors and parameters
        /// </summary>
        BaseParameters bp;
        ModelBaseParameters mbp;
        LoadBehavior load;
        TemperatureBehavior temp;
        ModelTemperatureBehavior modeltemp;

        /// <summary>
        /// Nodes
        /// </summary>
        int dNode, gNode, sNode, bNode, dNodePrime, sNodePrime;
        protected MatrixElement DdPtr { get; private set; }
        protected MatrixElement GgPtr { get; private set; }
        protected MatrixElement SsPtr { get; private set; }
        protected MatrixElement BbPtr { get; private set; }
        protected MatrixElement DPdpPtr { get; private set; }
        protected MatrixElement SPspPtr { get; private set; }
        protected MatrixElement DdpPtr { get; private set; }
        protected MatrixElement GbPtr { get; private set; }
        protected MatrixElement GdpPtr { get; private set; }
        protected MatrixElement GspPtr { get; private set; }
        protected MatrixElement SspPtr { get; private set; }
        protected MatrixElement BdpPtr { get; private set; }
        protected MatrixElement BspPtr { get; private set; }
        protected MatrixElement DPspPtr { get; private set; }
        protected MatrixElement DPdPtr { get; private set; }
        protected MatrixElement BgPtr { get; private set; }
        protected MatrixElement DPgPtr { get; private set; }
        protected MatrixElement SPgPtr { get; private set; }
        protected MatrixElement SPsPtr { get; private set; }
        protected MatrixElement DPbPtr { get; private set; }
        protected MatrixElement SPbPtr { get; private set; }
        protected MatrixElement SPdpPtr { get; private set; }

        /// <summary>
        /// Shared variables
        /// </summary>
        public double Capbs { get; protected set; }
        public double Capbd { get; protected set; }

        /// <summary>
        /// Constants
        /// </summary>
        StateHistory Vbs;
        StateHistory Vgs;
        StateHistory Vds;
        StateHistory Capgs;
        StateHistory Capgd;
        StateHistory Capgb;
        StateDerivative Qgs;
        StateDerivative Qgd;
        StateDerivative Qgb;
        StateDerivative Qbd;
        StateDerivative Qbs;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public TransientBehavior(Identifier name) : base(name) { }

        /// <summary>
        /// Setup behavior
        /// </summary>
        /// <param name="provider">Data provider</param>
        public override void Setup(SetupDataProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Get parameters
            bp = provider.GetParameterSet<BaseParameters>(0);
            mbp = provider.GetParameterSet<ModelBaseParameters>(1);

            // Get behaviors
            temp = provider.GetBehavior<TemperatureBehavior>(0);
            load = provider.GetBehavior<LoadBehavior>(0);
            modeltemp = provider.GetBehavior<ModelTemperatureBehavior>(1);
        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <param name="pins">Pins</param>
        public void Connect(params int[] pins)
        {
            if (pins == null)
                throw new ArgumentNullException(nameof(pins));
            if (pins.Length != 4)
                throw new Diagnostics.CircuitException("Pin count mismatch: 4 pins expected, {0} given".FormatString(pins.Length));
            dNode = pins[0];
            gNode = pins[1];
            sNode = pins[2];
            bNode = pins[3];
        }

        /// <summary>
        /// Get matrix pointers
        /// </summary>
        /// <param name="matrix">Matrix</param>
        public override void GetMatrixPointers(Matrix matrix)
        {
			if (matrix == null)
				throw new ArgumentNullException(nameof(matrix));

            // Get extra equations
            sNodePrime = load.SourceNodePrime;
            dNodePrime = load.DrainNodePrime;

            // Get matrix elements
            DdPtr = matrix.GetElement(dNode, dNode);
            GgPtr = matrix.GetElement(gNode, gNode);
            SsPtr = matrix.GetElement(sNode, sNode);
            BbPtr = matrix.GetElement(bNode, bNode);
            DPdpPtr = matrix.GetElement(dNodePrime, dNodePrime);
            SPspPtr = matrix.GetElement(sNodePrime, sNodePrime);
            DdpPtr = matrix.GetElement(dNode, dNodePrime);
            GbPtr = matrix.GetElement(gNode, bNode);
            GdpPtr = matrix.GetElement(gNode, dNodePrime);
            GspPtr = matrix.GetElement(gNode, sNodePrime);
            SspPtr = matrix.GetElement(sNode, sNodePrime);
            BdpPtr = matrix.GetElement(bNode, dNodePrime);
            BspPtr = matrix.GetElement(bNode, sNodePrime);
            DPspPtr = matrix.GetElement(dNodePrime, sNodePrime);
            DPdPtr = matrix.GetElement(dNodePrime, dNode);
            BgPtr = matrix.GetElement(bNode, gNode);
            DPgPtr = matrix.GetElement(dNodePrime, gNode);
            SPgPtr = matrix.GetElement(sNodePrime, gNode);
            SPsPtr = matrix.GetElement(sNodePrime, sNode);
            DPbPtr = matrix.GetElement(dNodePrime, bNode);
            SPbPtr = matrix.GetElement(sNodePrime, bNode);
            SPdpPtr = matrix.GetElement(sNodePrime, dNodePrime);
        }

        /// <summary>
        /// Unsetup the behavior
        /// </summary>
        public override void Unsetup()
        {
            // Remove references
            DdPtr = null;
            GgPtr = null;
            SsPtr = null;
            BbPtr = null;
            DPdpPtr = null;
            SPspPtr = null;
            DdpPtr = null;
            GbPtr = null;
            GdpPtr = null;
            GspPtr = null;
            SspPtr = null;
            BdpPtr = null;
            BspPtr = null;
            DPspPtr = null;
            DPdPtr = null;
            BgPtr = null;
            DPgPtr = null;
            SPgPtr = null;
            SPsPtr = null;
            DPbPtr = null;
            SPbPtr = null;
            SPdpPtr = null;
        }

        /// <summary>
        /// Create states
        /// </summary>
        /// <param name="states">States</param>
        public override void CreateStates(StatePool states)
        {
			if (states == null)
				throw new ArgumentNullException(nameof(states));

            Vbs = states.CreateHistory();
            Vgs = states.CreateHistory();
            Vds = states.CreateHistory();
            Capgs = states.CreateHistory();
            Capgd = states.CreateHistory();
            Capgb = states.CreateHistory();
            Qgs = states.CreateDerivative();
            Qgd = states.CreateDerivative();
            Qgb = states.CreateDerivative();
            Qbd = states.CreateDerivative();
            Qbs = states.CreateDerivative();
        }

        /// <summary>
        /// Calculate initial states
        /// </summary>
        /// <param name="simulation">Simulation</param>
        public override void GetDCstate(TimeSimulation simulation)
        {
			if (simulation == null)
				throw new ArgumentNullException(nameof(simulation));

            double EffectiveLength, GateSourceOverlapCap, GateDrainOverlapCap, GateBulkOverlapCap,
                OxideCap, vgs, vbs, vbd, vgb, vgd, von,
                vdsat, sargsw, capgs = 0.0, capgd = 0.0, capgb = 0.0;

            vbs = load.Vbs;
            vbd = load.Vbd;
            vgs = load.Vgs;
            vgd = load.Vgs - load.Vds;
            vgb = load.Vgs - load.Vbs;
            von = mbp.MosfetType * load.Von;
            vdsat = mbp.MosfetType * load.Vdsat;

            Vds.Current = load.Vds;
            Vbs.Current = vbs;
            Vgs.Current = vgs;

            EffectiveLength = bp.Length - 2 * mbp.LatDiff;
            GateSourceOverlapCap = mbp.GateSourceOverlapCapFactor * bp.Width;
            GateDrainOverlapCap = mbp.GateDrainOverlapCapFactor * bp.Width;
            GateBulkOverlapCap = mbp.GateBulkOverlapCapFactor * EffectiveLength;
            OxideCap = modeltemp.OxideCapFactor * EffectiveLength * bp.Width;

            /* 
            * now we do the hard part of the bulk - drain and bulk - source
            * diode - we evaluate the non - linear capacitance and
            * charge
            * 
            * the basic equations are not hard, but the implementation
            * is somewhat long in an attempt to avoid log / exponential
            * evaluations
            */
            /* 
            * charge storage elements
            * 
            * .. bulk - drain and bulk - source depletion capacitances
            */
            if (vbs < temp.TDepCap)
            {
                double arg = 1 - vbs / temp.TBulkPot, sarg;
                /* 
                * the following block looks somewhat long and messy, 
                * but since most users use the default grading
                * coefficients of .5, and sqrt is MUCH faster than an
                * Math.Exp(Math.Log()) we use this special case code to buy time.
                * (as much as 10% of total job time!)
                */
                if (mbp.BulkJctBotGradingCoeff.Value == mbp.BulkJctSideGradingCoeff)
                {
                    if (mbp.BulkJctBotGradingCoeff.Value == .5)
                    {
                        sarg = sargsw = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        sarg = sargsw = Math.Exp(-mbp.BulkJctBotGradingCoeff * Math.Log(arg));
                    }
                }
                else
                {
                    if (mbp.BulkJctBotGradingCoeff.Value == .5)
                    {
                        sarg = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sarg = Math.Exp(-mbp.BulkJctBotGradingCoeff * Math.Log(arg));
                    }
                    if (mbp.BulkJctSideGradingCoeff.Value == .5)
                    {
                        sargsw = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sargsw = Math.Exp(-mbp.BulkJctSideGradingCoeff * Math.Log(arg));
                    }
                }
                // NOSQRT
                Qbs.Current = temp.TBulkPot * (temp.Cbs * (1 - arg * sarg) / (1 - mbp.BulkJctBotGradingCoeff) +
                    temp.Cbssw * (1 - arg * sargsw) / (1 - mbp.BulkJctSideGradingCoeff));
            }
            else
            {
                Qbs.Current = temp.F4s + vbs * (temp.F2s + vbs * (temp.F3s / 2));
            }

            if (vbd < temp.TDepCap)
            {
                double arg = 1 - vbd / temp.TBulkPot, sarg;
                /* 
                * the following block looks somewhat long and messy, 
                * but since most users use the default grading
                * coefficients of .5, and sqrt is MUCH faster than an
                * Math.Exp(Math.Log()) we use this special case code to buy time.
                * (as much as 10% of total job time!)
                */
                if (mbp.BulkJctBotGradingCoeff.Value == .5 && mbp.BulkJctSideGradingCoeff.Value == .5)
                {
                    sarg = sargsw = 1 / Math.Sqrt(arg);
                }
                else
                {
                    if (mbp.BulkJctBotGradingCoeff.Value == .5)
                    {
                        sarg = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sarg = Math.Exp(-mbp.BulkJctBotGradingCoeff * Math.Log(arg));
                    }
                    if (mbp.BulkJctSideGradingCoeff.Value == .5)
                    {
                        sargsw = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sargsw = Math.Exp(-mbp.BulkJctSideGradingCoeff * Math.Log(arg));
                    }
                }
                /* NOSQRT */
                Qbd.Current = temp.TBulkPot * (temp.Cbd * (1 - arg * sarg) / (1 - mbp.BulkJctBotGradingCoeff) +
                    temp.Cbdsw * (1 - arg * sargsw) / (1 - mbp.BulkJctSideGradingCoeff));
            }
            else
            {
                Qbd.Current = temp.F4d + vbd * (temp.F2d + vbd * temp.F3d / 2);
            }

            /* 
             * calculate meyer's capacitors
             */
            double icapgs, icapgd, icapgb;
            if (load.Mode > 0)
            {
                Transistor.DEVqmeyer(vgs, vgd, von, vdsat,
                    out icapgs, out icapgd, out icapgb, temp.TPhi, OxideCap);
            }
            else
            {
                Transistor.DEVqmeyer(vgd, vgs, von, vdsat,
                    out icapgd, out icapgs, out icapgb, temp.TPhi, OxideCap);
            }
            Capgs.Current = icapgs;
            Capgd.Current = icapgd;
            Capgb.Current = icapgb;

            capgs = 2 * Capgs.Current + GateSourceOverlapCap;
            capgd = 2 * Capgd.Current + GateDrainOverlapCap;
            capgb = 2 * Capgb.Current + GateBulkOverlapCap;

            /* TRANOP */
            Qgs.Current = capgs * vgs;
            Qgd.Current = capgd * vgd;
            Qgb.Current = capgb * vgb;
        }

        /// <summary>
        /// Transient behavior
        /// </summary>
        /// <param name="simulation"></param>
        public override void Transient(TimeSimulation simulation)
        {
			if (simulation == null)
				throw new ArgumentNullException(nameof(simulation));

            var state = simulation.State;
            double EffectiveLength, GateSourceOverlapCap, GateDrainOverlapCap, GateBulkOverlapCap,
                OxideCap, vgs, vbs, vbd, vgb, vgd, von,
                vdsat, sargsw, vgs1, vgd1, vgb1, capgs = 0.0, capgd = 0.0, capgb = 0.0, gcgs, ceqgs, gcgd, ceqgd, gcgb, ceqgb, ceqbs,
                ceqbd;

            vbs = load.Vbs;
            vbd = load.Vbd;
            vgs = load.Vgs;
            vgd = load.Vgs - load.Vds;
            vgb = load.Vgs - load.Vbs;
            von = mbp.MosfetType * load.Von;
            vdsat = mbp.MosfetType * load.Vdsat;

            Vds.Current = load.Vds;
            Vbs.Current = vbs;
            Vgs.Current = vgs;

            double Gbd = 0.0;
            double Cbd = 0.0;
            double Cd = 0.0;
            double Gbs = 0.0;
            double Cbs = 0.0;

            EffectiveLength = bp.Length - 2 * mbp.LatDiff;
            GateSourceOverlapCap = mbp.GateSourceOverlapCapFactor * bp.Width;
            GateDrainOverlapCap = mbp.GateDrainOverlapCapFactor * bp.Width;
            GateBulkOverlapCap = mbp.GateBulkOverlapCapFactor * EffectiveLength;
            OxideCap = modeltemp.OxideCapFactor * EffectiveLength * bp.Width;

            /* 
            * now we do the hard part of the bulk - drain and bulk - source
            * diode - we evaluate the non - linear capacitance and
            * charge
            * 
            * the basic equations are not hard, but the implementation
            * is somewhat long in an attempt to avoid log / exponential
            * evaluations
            */
            /* 
            * charge storage elements
            * 
            * .. bulk - drain and bulk - source depletion capacitances
            */
            if (vbs < temp.TDepCap)
            {
                double arg = 1 - vbs / temp.TBulkPot, sarg;
                /* 
                * the following block looks somewhat long and messy, 
                * but since most users use the default grading
                * coefficients of .5, and sqrt is MUCH faster than an
                * Math.Exp(Math.Log()) we use this special case code to buy time.
                * (as much as 10% of total job time!)
                */
                if (mbp.BulkJctBotGradingCoeff.Value == mbp.BulkJctSideGradingCoeff)
                {
                    if (mbp.BulkJctBotGradingCoeff.Value == .5)
                    {
                        sarg = sargsw = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        sarg = sargsw = Math.Exp(-mbp.BulkJctBotGradingCoeff * Math.Log(arg));
                    }
                }
                else
                {
                    if (mbp.BulkJctBotGradingCoeff.Value == .5)
                    {
                        sarg = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sarg = Math.Exp(-mbp.BulkJctBotGradingCoeff * Math.Log(arg));
                    }
                    if (mbp.BulkJctSideGradingCoeff.Value == .5)
                    {
                        sargsw = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sargsw = Math.Exp(-mbp.BulkJctSideGradingCoeff * Math.Log(arg));
                    }
                }
                // NOSQRT
                Qbs.Current = temp.TBulkPot * (temp.Cbs * (1 - arg * sarg) / (1 - mbp.BulkJctBotGradingCoeff) +
                    temp.Cbssw * (1 - arg * sargsw) / (1 - mbp.BulkJctSideGradingCoeff));
                Capbs = temp.Cbs * sarg + temp.Cbssw * sargsw;
            }
            else
            {
                Qbs.Current = temp.F4s + vbs * (temp.F2s + vbs * (temp.F3s / 2));
                Capbs = temp.F2s + temp.F3s * vbs;
            }

            if (vbd < temp.TDepCap)
            {
                double arg = 1 - vbd / temp.TBulkPot, sarg;
                /* 
                * the following block looks somewhat long and messy, 
                * but since most users use the default grading
                * coefficients of .5, and sqrt is MUCH faster than an
                * Math.Exp(Math.Log()) we use this special case code to buy time.
                * (as much as 10% of total job time!)
                */
                if (mbp.BulkJctBotGradingCoeff.Value == .5 && mbp.BulkJctSideGradingCoeff.Value == .5)
                {
                    sarg = sargsw = 1 / Math.Sqrt(arg);
                }
                else
                {
                    if (mbp.BulkJctBotGradingCoeff.Value == .5)
                    {
                        sarg = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sarg = Math.Exp(-mbp.BulkJctBotGradingCoeff * Math.Log(arg));
                    }
                    if (mbp.BulkJctSideGradingCoeff.Value == .5)
                    {
                        sargsw = 1 / Math.Sqrt(arg);
                    }
                    else
                    {
                        /* NOSQRT */
                        sargsw = Math.Exp(-mbp.BulkJctSideGradingCoeff * Math.Log(arg));
                    }
                }
                /* NOSQRT */
                Qbd.Current = temp.TBulkPot * (temp.Cbd * (1 - arg * sarg) / (1 - mbp.BulkJctBotGradingCoeff) +
                    temp.Cbdsw * (1 - arg * sargsw) / (1 - mbp.BulkJctSideGradingCoeff));
                Capbd = temp.Cbd * sarg + temp.Cbdsw * sargsw;
            }
            else
            {
                Qbd.Current = temp.F4d + vbd * (temp.F2d + vbd * temp.F3d / 2);
                Capbd = temp.F2d + vbd * temp.F3d;
            }

            /* (above only excludes tranop, since we're only at this
            * point if tran or tranop)
            */
            /* 
            * calculate equivalent conductances and currents for
            * depletion capacitors
            */

            // integrate the capacitors and save results
            Qbd.Integrate();
            Gbd += Qbd.Jacobian(Capbd);
            Cbd += Qbd.Derivative;
            Cd -= Qbd.Derivative;
            Qbs.Integrate();
            Gbs += Qbs.Jacobian(Capbs);
            Cbs += Qbs.Derivative;

            /* 
             * calculate meyer's capacitors
             */
            double icapgs, icapgd, icapgb;
            if (load.Mode > 0)
            {
                Transistor.DEVqmeyer(vgs, vgd, von, vdsat,
                    out icapgs, out icapgd, out icapgb, temp.TPhi, OxideCap);
            }
            else
            {
                Transistor.DEVqmeyer(vgd, vgs, von, vdsat,
                    out icapgd, out icapgs, out icapgb, temp.TPhi, OxideCap);
            }
            Capgs.Current = icapgs;
            Capgd.Current = icapgd;
            Capgb.Current = icapgb;

            vgs1 = Vgs[1];
            vgd1 = vgs1 - Vds[1];
            vgb1 = vgs1 - Vbs[1];
            capgs = Capgs.Current + Capgs[1] + GateSourceOverlapCap;
            capgd = Capgd.Current + Capgd[1] + GateDrainOverlapCap;
            capgb = Capgb.Current + Capgb[1] + GateBulkOverlapCap;

            /* 
            * store small - signal parameters (for meyer's model)
            * all parameters already stored, so done...
            */

            Qgs.Current = (vgs - vgs1) * capgs + Qgs[1];
            Qgd.Current = (vgd - vgd1) * capgd + Qgd[1];
            Qgb.Current = (vgb - vgb1) * capgb + Qgb[1];

            /* 
             * calculate equivalent conductances and currents for
             * meyer"s capacitors
             */
            Qgs.Integrate();
            gcgs = Qgs.Jacobian(capgs);
            ceqgs = Qgs.RhsCurrent(gcgs, vgs);
            Qgd.Integrate();
            gcgd = Qgd.Jacobian(capgd);
            ceqgd = Qgd.RhsCurrent(gcgd, vgd);
            Qgb.Integrate();
            gcgb = Qgb.Jacobian(capgb);
            ceqgb = Qgb.RhsCurrent(gcgb, vgb);

            /* 
			* store charge storage info for meyer's cap in lx table
			*/

            /* 
            * load current vector
            */
            ceqbs = mbp.MosfetType * (Cbs - (Gbs - state.Gmin) * vbs);
            ceqbd = mbp.MosfetType * (Cbd - (Gbd - state.Gmin) * vbd);
            state.Rhs[gNode] -= (mbp.MosfetType * (ceqgs + ceqgb + ceqgd));
            state.Rhs[bNode] -= (ceqbs + ceqbd - mbp.MosfetType * ceqgb);
            state.Rhs[dNodePrime] += (ceqbd + mbp.MosfetType * ceqgd);
            state.Rhs[sNodePrime] += ceqbs + mbp.MosfetType * ceqgs;

            /* 
			 * load y matrix
			 */
            GgPtr.Add(gcgd + gcgs + gcgb);
            BbPtr.Add(Gbd + Gbs + gcgb);
            DPdpPtr.Add(Gbd + gcgd);
            SPspPtr.Add(Gbs + gcgs);
            GbPtr.Sub(gcgb);
            GdpPtr.Sub(gcgd);
            GspPtr.Sub(gcgs);
            BgPtr.Sub(gcgb);
            BdpPtr.Sub(Gbd);
            BspPtr.Sub(Gbs);
            DPgPtr.Add(-gcgd);
            DPbPtr.Add(-Gbd);
            SPgPtr.Add(-gcgs);
            SPbPtr.Add(-Gbs);
        }

        /// <summary>
        /// Truncate timestep
        /// </summary>
        /// <param name="timestep">Timestep</param>
        public override void Truncate(ref double timestep)
        {
            Qgs.LocalTruncationError(ref timestep);
            Qgd.LocalTruncationError(ref timestep);
            Qgb.LocalTruncationError(ref timestep);
        }
    }
}
