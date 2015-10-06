using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class UtilitiesShortcuts
{
	[MenuItem("Utilities/Shortcuts/Clear Console %#c")] // CTRL/CMD + SHIFT + C
	public static void ClearConsole()
	{
		try
		{
			var logEntries = Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");
			if(logEntries != null)
			{
				var method = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
				if(method != null)
				{
					method.Invoke(null, null);
				}
			}
		}
		catch(Exception exception)
		{
			Debug.LogError("Failed to clear the console: " + exception.ToString());
		}
	}

	[MenuItem("Utilities/Shortcuts/Save project &%s")] // ALT + CTRL + S
	static void SaveProject()
	{
		Debug.Log("Saved assets to disk.");
		EditorApplication.SaveAssets();
	}

	[MenuItem("Utilities/Shortcuts/Toggle Inspector Debug %#d")] // CTRL/CMD + SHIFT + C
	public static void ToggleInspectorDebug()
	{
		try
		{
			var type = Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
			if(type != null)
			{
				var window = EditorWindow.GetWindow(type);
				var field = type.GetField("m_InspectorMode", BindingFlags.Instance | BindingFlags.Public);
				if(field != null)
				{
					var mode = (InspectorMode)field.GetValue(window);
					var newMode = mode == InspectorMode.Debug ? InspectorMode.Normal : InspectorMode.Debug;

					var method = type.GetMethod("SetMode", BindingFlags.Instance | ~BindingFlags.Public);
					if(method != null)
					{
						method.Invoke(window, new object[] { newMode });
					}
				}
			}
		}
		catch(Exception exception)
		{
			Debug.LogError("Failed to toggle inspector debug: " + exception.ToString());
		}
	}

	[MenuItem("Utilities/Shortcuts/Toggle GameView maximized %#m")] // CTRL/CMD + SHIFT + M
	public static void ToggleGameViewMaximized()
	{
		try
		{
			var type = Type.GetType("UnityEditor.GameView,UnityEditor");
			if(type != null)
			{
				var window = EditorWindow.GetWindow(type);
				var property = type.GetProperty("maximized", BindingFlags.Instance | BindingFlags.Public);
				if(property != null)
				{
					var isMaximized = (bool)property.GetValue(window, null);
					property.SetValue(window, !isMaximized, null);
				}
			}
		}
		catch(Exception exception)
		{
			Debug.LogError("Failed to toggle GameView maximized: " + exception.ToString());
		}
	}

    [MenuItem("Utilities/Shortcuts/Toggle Inspector Lock %#l")] // CTRL/CMD + SHIFT + L
	public static void ToggleInspectorLock()
	{
		try
		{
			var type = Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
			if(type != null)
			{
				var window = EditorWindow.GetWindow(type);
				
				var method = type.GetMethod("FlipLocked", BindingFlags.Instance | ~BindingFlags.Public);
				if(method != null)
				{
					method.Invoke(window, null);
				}	
			}
		}
		catch(Exception exception)
		{
			Debug.LogError("Failed to toggle inspector debug: " + exception.ToString());
		}
	}

	public delegate void ApplyOrRevertDelegate(GameObject inInstance, UnityEngine.Object inPrefab, ReplacePrefabOptions inReplaceOptions);

	[MenuItem("Utilities/Shortcuts/Apply all selected prefabs %#e")] // CTRL/CMD + SHIFT + E
	static void ApplyPrefabs()
	{
		var count = SearchPrefabConnections((inInstance, inPrefab, inReplaceOptions) =>
			{
				PrefabUtility.ReplacePrefab(inInstance, inPrefab, inReplaceOptions);
			},
			"apply"
		);
		if(count > 0)
			SaveProject();
	}

	[MenuItem("Utilities/Shortcuts/Revert all selected prefabs &#r")] // ALT + SHIFT + R
	static void RevertPrefabs()
	{
		SearchPrefabConnections((inInstance, inPrefab, inReplaceOptions) =>
			{
				PrefabUtility.ReconnectToLastPrefab(inInstance);
				PrefabUtility.RevertPrefabInstance(inInstance);
			},
			"revert"
		);
	}

	static int SearchPrefabConnections(ApplyOrRevertDelegate inDelegate, string inDescriptor)
	{
		var count = 0;
		if(inDelegate != null)
		{
			var selectedGameObjects = Selection.gameObjects;
			if(selectedGameObjects.Length > 0)
			{
				foreach(var gameObject in selectedGameObjects)
				{
					var prefabType = PrefabUtility.GetPrefabType(gameObject);

					// Is the selected GameObject a prefab?
					if(prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.DisconnectedPrefabInstance)
					{
						// Get the prefab root.
						var prefabParent = ((GameObject)PrefabUtility.GetPrefabParent(gameObject));
						var prefabRoot = prefabParent.transform.root.gameObject;
						
						var currentGameObject = gameObject;
						var hasFoundTopOfHierarchy = false;
						var canApply = true;
						
						// We go up in the hierarchy until we locate a GameObject that doesn't have the same GetPrefabParent return value.
						while(currentGameObject.transform.parent && !hasFoundTopOfHierarchy)
						{
							// Same prefab?
							prefabParent = ((GameObject)PrefabUtility.GetPrefabParent(currentGameObject.transform.parent.gameObject));
							if(prefabParent && prefabRoot == prefabParent.transform.root.gameObject)
							{
								// Continue upwards.
								currentGameObject = currentGameObject.transform.parent.gameObject;
							}
							else
							{
								// The gameobject parent is another prefab, we stop here.
								hasFoundTopOfHierarchy = true;
								if(prefabRoot != ((GameObject)PrefabUtility.GetPrefabParent(currentGameObject)))
								{
									// Gameobject is part of another prefab.
									canApply = false;
								}
							}
						}

						if(canApply)
						{
							count++;
							var parent = PrefabUtility.GetPrefabParent(currentGameObject);
							inDelegate(currentGameObject, parent, ReplacePrefabOptions.ConnectToPrefab);
							var assetPath = AssetDatabase.GetAssetPath(parent);
							Debug.Log(assetPath + " " + inDescriptor, parent);
						}
					}
				}
				Debug.Log(count + " prefab" + (count > 1 ? "s" : "") + " updated");
			}
		}

		return count;
	}
}