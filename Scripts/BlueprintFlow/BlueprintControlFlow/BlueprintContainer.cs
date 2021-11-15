namespace MechSharingCode.Blueprints.BlueprintControlFlow
{
    // using System;
    // using System.Collections.Generic;
    // using Mech.Core.BlueprintFlow.BlueprintReader;
    //
    // /// <summary>
    // /// A container include all instances of blueprint data as both list and dictionary
    // /// </summary>
    // public class BlueprintContainer
    // {
    //     private readonly Dictionary<Type, IGenericBlueprint> dictBlueprint;
    //
    //     public BlueprintContainer(List<IGenericBlueprint> listBlueprints)
    //     {
    //         this.AllBlueprints  = listBlueprints;
    //         this.dictBlueprint = new Dictionary<Type, IGenericBlueprint>();
    //         foreach (var blueprintReader in listBlueprints)
    //         {
    //             this.dictBlueprint[blueprintReader.GetType()] = blueprintReader;
    //         }
    //     }
    //
    //     /// <summary>
    //     /// Get a blueprint instance
    //     /// </summary>
    //     /// <typeparam name="TBlueprint"> type of blueprint </typeparam>
    //     public TBlueprint GetBlueprint<TBlueprint>() where TBlueprint : IGenericBlueprint
    //     {
    //         if (this.dictBlueprint.TryGetValue(typeof(TBlueprint), out var result))
    //         {
    //             return (TBlueprint)result;
    //         }
    //
    //         return default;
    //     }
    //
    //     public List<IGenericBlueprint> AllBlueprints { get; }
    // }
}