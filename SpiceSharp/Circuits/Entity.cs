﻿using System;
using System.Collections.Generic;
using System.Threading;
using SpiceSharp.Behaviors;
using SpiceSharp.Simulations;

namespace SpiceSharp.Circuits
{
    /// <summary>
    /// Base class for any circuit object that can take part in simulations.
    /// </summary>
    /// <remarks>
    /// Entities should not contain references to other entities, but only their name identifiers. In the method
    /// <see cref="BindBehavior"/>  the entity should try to find the necessary behaviors and parameters 
    /// generated by other entities and pass them via a <see cref="BindingContext"/>.
    /// </remarks>
    public abstract class Entity : ICloneable, ICloneable<Entity>
    {
        private static Dictionary<Type, BehaviorFactoryDictionary> BehaviorFactories { get; } =
            new Dictionary<Type, BehaviorFactoryDictionary>();
        private static ReaderWriterLockSlim Lock { get; } = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Registers a behavior factory for an entity type.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="dictionary">The dictionary.</param>
        protected static void RegisterBehaviorFactory(Type entityType, BehaviorFactoryDictionary dictionary)
        {
            Lock.EnterWriteLock();
            try
            {
                BehaviorFactories.Add(entityType, dictionary);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets a collection of parameters.
        /// </summary>
        public ParameterSetDictionary ParameterSets { get; } = new ParameterSetDictionary();

        /// <summary>
        /// Gets the name of the entity.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        protected Entity(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Sets the principal parameter.
        /// </summary>
        /// <typeparam name="T">The base value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if a principal parameter was set; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Only the first encountered principal parameter will be set.
        /// </remarks>
        public bool SetPrincipalParameter<T>(T value) => ParameterSets.SetPrincipalParameter(value);

        /// <summary>
        /// Runs a method with a specific parameter name.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> implementation to use when comparing parameter names, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/>.</param>
        public Entity SetParameter(string name, IEqualityComparer<string> comparer = null)
        {
            ParameterSets.SetParameter(name, comparer);
            return this;
        }

        /// <summary>
        /// Sets a parameter with a specific name.
        /// </summary>
        /// <typeparam name="T">The base value type.</typeparam>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> implementation to use when comparing parameter names, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/>.</param>
        /// <returns>False if the parameter could not be found.</returns>
        public Entity SetParameter<T>(string name, T value, IEqualityComparer<string> comparer = null)
        {
            ParameterSets.SetParameter(name, value, comparer);
            return this;
        }

        /// <summary>
        /// Gets the principal parameter.
        /// </summary>
        /// <typeparam name="T">The base value type.</typeparam>
        /// <returns>
        /// The principal parameter of the specified type.
        /// </returns>
        public T GetPrincipalParameter<T>() => ParameterSets.GetPrincipalParameter<T>();

        /// <summary>
        /// Gets a parameter with a specified name.
        /// </summary>
        /// <typeparam name="T">The base value type.</typeparam>
        /// <param name="name">The parameter name.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> implementation to use when comparing parameter names, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/>.</param>
        public T GetParameter<T>(string name, IEqualityComparer<string> comparer = null) => ParameterSets.GetParameter<T>(name, comparer);

        /// <summary>
        /// Creates behaviors of the specified types. The type order is important.
        /// </summary>
        /// <remarks>
        /// The order typically indicates hierarchy. The entity will create the behaviors in reverse order, allowing
        /// the most specific child class to be used that is necessary. For example, the <see cref="OP"/> simulation needs
        /// <see cref="ITemperatureBehavior"/> and an <see cref="IBiasingBehavior"/>. The entity will first look for behaviors
        /// of type <see cref="IBiasingBehavior"/>, and then for the behaviors of type <see cref="ITemperatureBehavior"/>. However,
        /// if the behavior that was created for <see cref="IBiasingBehavior"/> also implements <see cref="ITemperatureBehavior"/>,
        /// then then entity will not create a new instance of the behavior.
        /// </remarks>
        /// <param name="types">The types of behaviors that the simulation wants, in the order that they will be called.</param>
        /// <param name="simulation">The simulation requesting the behaviors.</param>
        /// <param name="entities">The entities being processed, used by the entity to find linked entities.</param>
        public virtual void CreateBehaviors(Type[] types, Simulation simulation, EntityCollection entities)
        {
            types.ThrowIfNull(nameof(types));
            simulation.ThrowIfNull(nameof(simulation));
            entities.ThrowIfNull(nameof(entities));

            // Skip creating behaviors if the entity is already defined in the pool
            var pool = simulation.EntityBehaviors;
            if (pool.ContainsKey(Name))
                return;

            // Get the behavior factories for this entity
            BehaviorFactoryDictionary factories;
            Lock.EnterReadLock();
            try
            {
                if (!BehaviorFactories.TryGetValue(GetType(), out factories))
                    return;
            }
            finally
            {
                Lock.ExitReadLock();
            }

            // By default, go through the types in reverse order (to account for inheritance) and create
            // the behaviors
            EntityBehaviorDictionary ebd = null;
            var newBehaviors = new List<IBehavior>(types.Length);
            for (var i = types.Length - 1; i >= 0; i--)
            {
                // Skip creating behaviors that aren't needed
                if (ebd != null && ebd.ContainsKey(types[i]))
                    continue;
                Lock.EnterReadLock();
                try
                {
                    if (factories.TryGetValue(types[i], out var factory))
                    {
                        // Create the behavior
                        var behavior = factory(this);
                        pool.Add(behavior);
                        newBehaviors.Add(behavior);

                        // Get the dictionary if necessary
                        if (ebd == null)
                            ebd = pool[Name];
                    }
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }

            // Now set them up in the order they appear
            for (var i = newBehaviors.Count - 1; i >= 0; i--)
                BindBehavior(newBehaviors[i], simulation);
        }
        
        /// <summary>
        /// Binds the behavior to the simulation.
        /// </summary>
        /// <param name="behavior">The behavior that needs to be bound to the simulation.</param>
        /// <param name="simulation">The simulation to be bound to.</param>
        protected virtual void BindBehavior(IBehavior behavior, Simulation simulation)
        {
            simulation.ThrowIfNull(nameof(simulation));

            // Build the setup behavior
            var context = new BindingContext();

            // Add existing entity behaviors that have already been created
            context.Add("entity", simulation.EntityBehaviors[Name]);
            context.Add("entity", simulation.EntityParameters[Name]);

            // Finally bind the behavior to the simulation
            behavior.Bind(simulation, context);
        }

        /// <summary>
        /// Clone the entity
        /// </summary>
        /// <returns></returns>
        public virtual Entity Clone()
        {
            var clone = (Entity) Activator.CreateInstance(GetType(), Name);
            clone.CopyFrom(this);
            return clone;
        }

        /// <summary>
        /// Clone the entity for instancing a circuit as a subcircuit.
        /// </summary>
        /// <param name="data">The instancing data.</param>
        /// <returns></returns>
        public virtual Entity Clone(InstanceData data)
        {
            data.ThrowIfNull(nameof(data));
            var clone = (Entity)Activator.CreateInstance(GetType(), data.GenerateIdentifier(Name));
            clone.CopyFrom(this);
            return clone;
        }

        /// <summary>
        /// Clone this object.
        /// </summary>
        ICloneable ICloneable.Clone() => Clone();

        /// <summary>
        /// Copy properties from another entity.
        /// </summary>
        /// <param name="source">The source entity.</param>
        public virtual void CopyFrom(Entity source)
        {
            source.ThrowIfNull(nameof(source));
            Reflection.CopyPropertiesAndFields(source, this);
        }

        /// <summary>
        /// Copy properties from another object.
        /// </summary>
        /// <param name="source">The source object.</param>
        void ICloneable.CopyFrom(ICloneable source) => CopyFrom((Entity)source);
    }
}
