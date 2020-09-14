#if UNITY_5_2 || UNITY_5_3_OR_NEWER
#define UNITY_5_2_OR_NEWER
#endif

#if UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2_OR_NEWER
#define UNITY_4_6_OR_NEWER
#endif

#if UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6_OR_NEWER
#define UNITY_4_3_OR_NEWER
#endif

#if UNITY_3_5 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3_OR_NEWER
#define UNITY_3_5_OR_NEWER
#endif

using System.Linq;
using UnityEditor;
using UnityEngine;

public class RealAssetSizeWindow : EditorWindow {
	
	private enum SelectionMode { NoObjects, MultipleObjects, InappropriateObject, Sprite, Mesh }
	private static SelectionMode _selectionMode;
	
	// Sprites were only added in 4.3
	#if UNITY_4_3_OR_NEWER
	private static Sprite _spr;
	#endif
	private static Mesh _mesh;
	private static Transform _transform;
	
	private static bool _lockEditing;
	
	private const string PathToDocumentation = "/Real Asset Size/Documentation.pdf";

	[MenuItem("Window/Real Asset Size")]
	private static void Init() {
		var window = (RealAssetSizeWindow)GetWindow(typeof(RealAssetSizeWindow), false, "Real Asset Size");
		window.Show();
		window.minSize = new Vector2(140f, 30f);

		// Selection.selectionChanged was added in 5.2, but it doesn't seem to work that great until 2017 so we use the old method until then.
		#if UNITY_2017_1_OR_NEWER
		Selection.selectionChanged = null;
		Selection.selectionChanged += SelectionChanged;
		#endif
	}

	[MenuItem("Help/Real Asset Size")]
	private static void OpenDocumentation() {
		Application.OpenURL(Application.dataPath + PathToDocumentation);
	}

	private void OnGUI() {
		
		var guiState = GUI.enabled;
		
		// 2018.2+ we use _lockEditing to make sure the player isn't editing a Prefab in Project View - only as an instance or in Prefab View
		#if UNITY_2018_2_OR_NEWER
		if (_lockEditing) {
			GUI.enabled = false;
		}
		#endif

		// Label width works differently before 4.3 so we don't need this variable
		#if UNITY_4_3_OR_NEWER
		float defaultLabelWidth;
		#endif
		
		GUILayout.Space(5f);
		
		switch (_selectionMode) {
			case SelectionMode.NoObjects:
				break;
			case SelectionMode.MultipleObjects:
				GUILayout.Label("Multi Object Editing is not supported");
				break;
			case SelectionMode.InappropriateObject:
				GUILayout.Label("This type of object isn't supported. See documentation for more info (Help > Real Asset Size)"); // Comment out this line if you don't want it to display a message for unsupported object types.
				break;
			case SelectionMode.Sprite:
				
				// Sprites were only added in 4.3. We shouldn't be in this case if it's an older version
				#if UNITY_4_3_OR_NEWER
				defaultLabelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 20f;

				if (_transform != null) {

					if (_spr == null) {
						_selectionMode = SelectionMode.InappropriateObject;
						break;
					}
				
					var lossyScale = _transform.lossyScale;
					var unitWidth = _spr.rect.width / _spr.pixelsPerUnit;
					var w = unitWidth * lossyScale.x;
					var unitHeight = _spr.rect.height / _spr.pixelsPerUnit;
					var h = unitHeight * lossyScale.y;

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.BeginHorizontal();
					
					GUILayout.FlexibleSpace();
					w = EditorGUILayout.FloatField("X", w, GUILayout.MaxWidth(Screen.width / 2.5f));
					h = EditorGUILayout.FloatField("Y", h, GUILayout.MaxWidth(Screen.width / 2.5f));
					GUILayout.FlexibleSpace();
					
					EditorGUILayout.EndHorizontal();

					if (EditorGUI.EndChangeCheck()) {

						Vector3 newSize;
						
						#if UNITY_2018_2_OR_NEWER
						#else
						if (_lockEditing) {
							Undo.RecordObject(_transform, "Changed Scale of Object (" + _transform.name + ")");
							newSize = new Vector3(w / unitWidth, h / unitHeight, _transform.lossyScale.z);
							_transform.localScale = newSize;
							break;
						}
						#endif
						
						Undo.RecordObject(_transform, "Changed Scale of Object (" + _transform.name + ")");
						newSize = new Vector3(w / unitWidth, h / unitHeight, _transform.lossyScale.z);
						var parent = _transform.parent;
						_transform.SetParent(null);
						_transform.localScale = newSize;
						_transform.SetParent(parent); 
					}

					if (_transform.lossyScale != Vector3.one) {
						EditorGUILayout.HelpBox("It can cause physics problems when objects aren't at 1,1,1 scale. If you experience problems, try resizing the image in your image editor instead of resizing it in Unity", MessageType.Warning);
					}
				} else {
					guiState = GUI.enabled;
					GUI.enabled = false;
					EditorGUILayout.BeginHorizontal();
					
					GUILayout.FlexibleSpace();
					EditorGUILayout.FloatField("X", _spr.rect.width / _spr.pixelsPerUnit, GUILayout.MaxWidth(Screen.width / 2.5f));
					EditorGUILayout.FloatField("Y", _spr.rect.height / _spr.pixelsPerUnit, GUILayout.MaxWidth(Screen.width / 2.5f));
					GUILayout.FlexibleSpace();
					
					EditorGUILayout.EndHorizontal();
					GUI.enabled = guiState;
				}
				EditorGUIUtility.labelWidth = defaultLabelWidth;
				break;
				#else
				Debug.LogError("Type is set to sprite, but sprite is not added in this version of Unity (using " + Application.unityVersion + "; was added in 4.3)");
				break;
				#endif
			case SelectionMode.Mesh:
				
				// Label Width was only added in 4.3, before that we use LookLikeControls to set the labelWidth, the field width is overwritten in the float fields.
				#if UNITY_4_3_OR_NEWER
				defaultLabelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 20f;
				#else
				EditorGUIUtility.LookLikeControls(labelWidth: 20f, fieldWidth: 0f);
				#endif
				
				if (_transform != null) {

					if (_mesh == null) {
						_selectionMode = SelectionMode.InappropriateObject;
						break;
					}

					var lossyScale = _transform.lossyScale;
					var x = _mesh.bounds.size.x * lossyScale.x;
					var y = _mesh.bounds.size.y * lossyScale.y;
					var z = _mesh.bounds.size.z * lossyScale.z;

					// Change Checks were only added in 3.5. See below.
					#if UNITY_3_5_OR_NEWER
					EditorGUI.BeginChangeCheck();
					#endif
					EditorGUILayout.BeginHorizontal();
					
					GUILayout.FlexibleSpace();
					x = EditorGUILayout.FloatField("X", x, GUILayout.MaxWidth(Screen.width / 3.5f));
					y = EditorGUILayout.FloatField("Y", y, GUILayout.MaxWidth(Screen.width / 3.5f));
					z = EditorGUILayout.FloatField("Z", z, GUILayout.MaxWidth(Screen.width / 3.5f));
					GUILayout.FlexibleSpace();
					
					EditorGUILayout.EndHorizontal();

					// Change Checks were only added in 3.5, so for 3.4 we use GUI.changed to check for changes instead. It seems to work okay.
					#if UNITY_3_5_OR_NEWER
					if (EditorGUI.EndChangeCheck()) {
					#else
					if (GUI.changed) {
					#endif

						Vector3 newSize;
						
						// If this is before the introduction of Prefab View in 2018.2 and it's locked for editing, we shouldn't change the parent
						#if UNITY_2018_2_OR_NEWER
						#else
						if (_lockEditing) {
							
							#if UNITY_4_3_OR_NEWER
							Undo.RecordObject(_transform, "Changed Scale of Object (" + _transform.name + ")");
							#else
							Undo.RegisterUndo(_transform, "Changed Scale of Object (" + _transform.name + ")");
							#endif
							
							newSize = new Vector3(x / _mesh.bounds.size.x, y / _mesh.bounds.size.y, z / _mesh.bounds.size.z);
							_transform.localScale = newSize;
							break;
						}
						#endif
						
						// From 4.3 onwards you record objects to handle undos, before that, you register an undo.
						#if UNITY_4_3_OR_NEWER
						Undo.RecordObject(_transform, "Changed Scale of Object (" + _transform.name + ")");
						#else
						Undo.RegisterUndo(_transform, "Changed Scale of Object (" + _transform.name + ")");
						#endif
						
						newSize = new Vector3(x / _mesh.bounds.size.x, y / _mesh.bounds.size.y, z / _mesh.bounds.size.z);
						var parent = _transform.parent;
						
						#if UNITY_4_6_OR_NEWER
						_transform.SetParent(null);
						_transform.localScale = newSize;
						_transform.SetParent(parent);
						#else
						// I prefer using the .SetParent method - but we set the variable before 4.6 when the method was added.
						_transform.parent = null;
						_transform.localScale = newSize;
						_transform.parent = parent;
						#endif
					}
					
					if (_transform.lossyScale != Vector3.one) {
						// Help Boxes were added in 3.5, so in 3.4 we just display it as a Label instead (we have to change the width first as we set it to 20f further up) 
						#if UNITY_3_5_OR_NEWER
						EditorGUILayout.HelpBox("It can cause physics problems when objects aren't at 1,1,1 scale. If you experience problems, resize the mesh in your modelling program instead of resizing it in Unity", MessageType.Warning);
						#else
						EditorGUIUtility.LookLikeControls(labelWidth: Screen.width, fieldWidth: 0f);
						EditorGUILayout.LabelField("It can cause physics problems when objects aren't at 1,1,1 scale. If you experience problems, resize the mesh in your modelling program instead of resizing it in Unity", "");
						#endif
					}
				} else {
					guiState = GUI.enabled;
					GUI.enabled = false;
					EditorGUILayout.BeginHorizontal();
					
					GUILayout.FlexibleSpace();
					EditorGUILayout.FloatField("X", _mesh.bounds.size.x, GUILayout.MaxWidth(Screen.width / 3.5f));
					EditorGUILayout.FloatField("Y", _mesh.bounds.size.y, GUILayout.MaxWidth(Screen.width / 3.5f));
					EditorGUILayout.FloatField("Z", _mesh.bounds.size.z, GUILayout.MaxWidth(Screen.width / 3.5f));
					GUILayout.FlexibleSpace();
					
					EditorGUILayout.EndHorizontal();
					GUI.enabled = guiState;
				}
				
				// If we're using Unity 4.3 or newer, we reset the label width. In the older system, there doesn't look to be a way to grab the default, but we set it before we use it anyway so it should be fine.
				#if UNITY_4_3_OR_NEWER
				EditorGUIUtility.labelWidth = defaultLabelWidth;
				#endif
				
				break;
			default:
				Debug.LogError("Unexpected Selection Mode: " + _selectionMode);
				break;
		}

		// If we're using the 2018.2 version of lockEditing, reset the GUI enabled to how it was before we changed it.
		#if UNITY_2018_2_OR_NEWER
		if (_lockEditing) {
			GUI.enabled = guiState;
		}
		#endif
	}

	private void OnInspectorUpdate() {
		Repaint();
		
		// This prevents error occuring if the user changes stuff about the image, such as converting it from a Sprite into another type of texture.
		// Also by checking whether it's a texture rather than checking the _selectionMode as Sprite, we are checking even if the texture isn't a sprite in case the user turns it into a sprite
		#if UNITY_4_3_OR_NEWER
		if (Selection.objects.Length > 0 && Selection.objects[0] is Texture2D) {
			var selected = Selection.objects;
			Selection.objects = new Object[0];
			Selection.objects = selected;
			SelectionChanged();
		}
		#endif
		
		// The select invoke list goes a bit weird when code gets recompiled, so we check if it's != 1 and if we, we clear it and add ourselves again. In older versions before the
		// event got added, we instead call this every OnInspectorUpdate frame. It has no noticeable performance impact on my end but we use the more elegant solution where possible.
		#if UNITY_2017_1_OR_NEWER
		if (Selection.selectionChanged.GetInvocationList().Length != 1) {
			Selection.selectionChanged = null;
			Selection.selectionChanged += SelectionChanged;
			SelectionChanged();
		}
		#else
		SelectionChanged();
		#endif
	}

	private static void SelectionChanged() {
		var allObjs = Selection.objects;
		_lockEditing = false;

		if (allObjs.Length == 0) {
			_selectionMode = SelectionMode.NoObjects;
		} else if (allObjs.Length > 1) {
			_selectionMode = SelectionMode.MultipleObjects;
		} else {
			var sceneObjs = Selection.transforms;

			if (sceneObjs.Length == 1) {
				_transform = sceneObjs[0];
				GetComponentFromObj(sceneObjs[0]);
			} else {
				_transform = null;

				if (allObjs[0] == null) {
					_selectionMode = SelectionMode.InappropriateObject;
					return;
				}
				
				var objType = allObjs[0].GetType();

				if (objType == typeof(GameObject)) {
					var go = allObjs[0] as GameObject;
					if (go == null) {
						Debug.LogError("objType is GameObject, but we cannot find a GameObject");
						_selectionMode = SelectionMode.InappropriateObject;
						return;
					}

					_lockEditing = true;
					
					#if UNITY_2018_2_OR_NEWER // In 2018.2 and newer, it works fine: models cannot be edited, prefabs can
					_transform = go.transform;
					#elif UNITY_3_5_OR_NEWER // In 3.5 to 2018.1, we need to check that it's not a ModelPrefab, else models can be edited which isn't good.
					if (PrefabUtility.GetPrefabType(go) != PrefabType.ModelPrefab) {
						_transform = go.transform;
					}
					#else // If in 3.4, we use EditorUtility rather than PrefabUtility.
					if (EditorUtility.GetPrefabType(go) != PrefabType.ModelPrefab) {
						_transform = go.transform;
					}
					#endif
					
					GetComponentFromObj(go.transform);
				}
				// The else condition for Sprites and Texture2Ds. Sprites don't exist before 4.3, so we don't perform these checks.
				#if UNITY_4_3_OR_NEWER
				else if (objType == typeof(Sprite)) {
					var spr = allObjs[0] as Sprite;
					if (spr == null) {
						Debug.LogError("objType is Sprite, but we cannot find a Sprite");
						_selectionMode = SelectionMode.InappropriateObject;
						return;
					}

					_spr = spr;
					_selectionMode = SelectionMode.Sprite;
				} else if (objType == typeof(Texture2D)) {
					var tex = allObjs[0] as Texture2D;
					if (tex == null) {
						Debug.LogError("objType is Texture2D, but we cannot find a Texture2D");
						_selectionMode = SelectionMode.InappropriateObject;
						return;
					}
					
					var path = AssetDatabase.GetAssetPath(tex);
					Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();

					if (sprites.Length == 0) {
						_selectionMode = SelectionMode.InappropriateObject;
					} else if (sprites.Length > 1) {
						_selectionMode = SelectionMode.MultipleObjects;
					} else {
						_spr = sprites[0];
						_selectionMode = SelectionMode.Sprite;
					}
				} 
				#endif
				else if (objType == typeof(Mesh)) {
					var mesh = allObjs[0] as Mesh;
					if (mesh == null) {
						Debug.LogError("objType is Mesh, but we cannot find a Mesh");
						_selectionMode = SelectionMode.InappropriateObject;
						return;
					}

					_mesh = mesh;
					_selectionMode = SelectionMode.Mesh;
				} else {
					_selectionMode = SelectionMode.InappropriateObject;
				}
			}
		}
	}

	private static void GetComponentFromObj(Component obj) {
		var tAndC = GetObjectType(obj);
		var type = tAndC.GetObjType();
		var component = tAndC.GetTheComponent();

		switch (type) {
			case ObjType.None:
				_selectionMode = SelectionMode.InappropriateObject;
				break;
			case ObjType.SpriteRenderer:
				// Sprites don't exist before 4.3, so we don't have a SpriteRenderer
				#if UNITY_4_3_OR_NEWER
				var sr = component as SpriteRenderer;
				if (sr == null) {
					Debug.LogError("It returned as a SpriteRenderer, but there is no SpriteRenderer attached");
					_selectionMode = SelectionMode.InappropriateObject;
					break;
				}
				
				_spr = sr.sprite;
				_selectionMode = SelectionMode.Sprite;
				break;
			#else
			Debug.LogError("We seem to think we have a Sprite Renderer, but this is a version of Unity before Sprites were added. Version: " + Application.unityVersion + ", Sprites added: 4.3");
			break;
			#endif
			case ObjType.MeshFilter:
				var mf = component as MeshFilter;
				if (mf == null) {
					Debug.LogError("It returned as a MeshFilter, but there is no MeshFilter attached");
					_selectionMode = SelectionMode.InappropriateObject;
					break;
				}

				_mesh = mf.sharedMesh;
				_selectionMode = SelectionMode.Mesh;
				break;
			case ObjType.SkinnedMeshRenderer:
				var smr = component as SkinnedMeshRenderer;
				if (smr == null) {
					Debug.LogError("It returned as a SkinnedMeshRenderer, but there is no SkinnedMeshRenderer attached");
					_selectionMode = SelectionMode.InappropriateObject;
					break;
				}
				_transform = null;
				_mesh = smr.sharedMesh;
				_selectionMode = SelectionMode.Mesh;
				break;
			default:
				Debug.LogError("Unexpected Type " + type);
				_selectionMode = SelectionMode.InappropriateObject;
				break;
		}
	}

	private enum ObjType { None, SpriteRenderer, MeshFilter, SkinnedMeshRenderer }

	private static TypeAndComponent GetObjectType(Component obj) {

		ObjType type = ObjType.None;
		Component component = null;

		// We don't need to check for a SpriteRenderer before 4.3 because Sprites don't exist.
		#if UNITY_4_3_OR_NEWER
		var sr = obj.GetComponent<SpriteRenderer>();

		if (sr != null) {
			component = sr;
			 type = ObjType.SpriteRenderer;
		}
		#endif
		
		var mf = obj.GetComponent<MeshFilter>();

		if (mf != null) {
			component = mf;
			type = ObjType.MeshFilter;
		}

		var smr = obj.GetComponent<SkinnedMeshRenderer>();

		if (smr != null) {
			component = smr;
			type = ObjType.SkinnedMeshRenderer;
		}

		return new TypeAndComponent(type, component);
	}

	
	// C# 4.0 (Unity 2017 and before) doesn't support out variables, so we return a class instead so we can grab both the type and the component.
	[System.Serializable]
	private class TypeAndComponent {
		private readonly ObjType _type;
		private readonly Component _component;

		public TypeAndComponent(ObjType type, Component component) {
			_type = type;
			_component = component;
		}

		public ObjType GetObjType() {
			return _type;
		}

		public Component GetTheComponent() {
			return _component;
		}
	}
}
