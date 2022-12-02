using UnityEngine;
using System;
using UnityEditor;
using System.Reflection;

public class RelationsInspectorLink
{
	// assembly names
	const string riAssemblyName = "RelationsInspector";
	const string editorAssemblyName = "Assembly-CSharp-Editor";
	const string editorFirstPassAssemblyName = "Assembly-CSharp-Editor-firstpass";

	// window type
	const string riWindowTypeName = "RelationsInspector.RelationsInspectorWindow";
	static Type windowType;

	// window's API1 property
	const string api1PropertyName = "GetAPI1";
	static PropertyInfo api1Property;

	// API1 type
	const string riAPI1TypeName = "RelationsInspector.RelationsInspectorAPI";
	static Type api1Type;

	// API1's ResetTargets method
	const string api1ResetTargetsMethodName = "ResetTargets";
	static Type[] api1ResetTargetsArguments = new Type[] { typeof( object[] ), typeof( Type ), typeof( bool ) };
	static MethodInfo api1ResetTargetsMethod;

	// RI is available iff all types, properties and methods could be retrieved
	public static bool RIisAvailable
	{
		get; private set;
	}

	// ctor. retrieves types, properties and methods
	static RelationsInspectorLink()
	{
		windowType = GetTypeInAssembly( riWindowTypeName, riAssemblyName );
		if ( windowType == null )
		{
			return; // this happens when RI is not installed. no need for an error msg here.
		}

		api1Property = windowType.GetProperty( api1PropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty );
		if ( api1Property == null )
		{
			Debug.LogError( "Failed to retrieve API1 property of type " + windowType );
			return;
		}

		api1Type = GetTypeInAssembly( riAPI1TypeName, riAssemblyName );
		if ( api1Type == null )
		{
			Debug.LogError( "Failed to retrieve API1 type" );
			return;
		}

		api1ResetTargetsMethod = api1Type.GetMethod( api1ResetTargetsMethodName, api1ResetTargetsArguments );
		if ( api1ResetTargetsMethod == null )
		{
			Debug.LogError( "Failed to retrieve API method ResetTargets(object[],Type,bool)" );
			return;
		}

		RIisAvailable = true;
	}

	// opens the window and returns its API1 object
	static object GetAPI1Object()
	{
		if ( !RIisAvailable )
			throw new Exception( "RelationsInspector is not available" );

		var windowObj = EditorWindow.GetWindow( windowType );
		if ( windowObj == null )
		{
			Debug.LogWarning( "failed to get window of type " + windowType );
			return null;
		}

		return api1Property.GetValue( windowObj, null );
	}

	// calls ResetTargets. for backends that are shipped with RI
	public static void ResetTargets( object[] targets, string backendTypeName, bool delayed = true )
	{
		if ( !RIisAvailable )
			throw new Exception( "RelationsInspector is not available" );

		Type backendType = GetTypeInAssembly( backendTypeName, editorAssemblyName );
	    if (backendType == null)
	    {
	        backendType = GetTypeInAssembly(backendTypeName, editorFirstPassAssemblyName);
	    }

	    if ( backendType == null )
		{
			Debug.LogError( "Failed to retrieve backend type " + backendTypeName );
			return;
		}

		object api1 = GetAPI1Object();
		api1ResetTargetsMethod.Invoke( api1, new object[] { targets, backendType, delayed } );
	}

	// retrieves the type from the assembly. names are case-sensitive.
	// returns null if the type was not found
	static Type GetTypeInAssembly( string typeName, string assemblyName )
	{
		return Type.GetType( typeName + "," + assemblyName, false, false );
	}
}

