using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(SpriteDataSO.Frame))]
public class SpriteSOPropertyDrawer : PropertyDrawer
{
	private int ElementSpacing = 25;
	public static Type TupleTypeReference = Type.GetType("System.ITuple, mscorlib");
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		// Using BeginProperty / EndProperty on the parent property means that
		// prefab override logic works on the entire property.
		EditorGUI.BeginProperty(position, label, property);

		//// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't make child fields be indented
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		// Calculate rects
		SerializedProperty texProp = property.FindPropertyRelative("spriteName");
		float texPropHeight = EditorGUI.GetPropertyHeight(texProp);
		var amountRect = new Rect(position.x, position.y, position.width, texPropHeight);

		// //Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.BeginChangeCheck();
		EditorGUI.PropertyField(amountRect, texProp, GUIContent.none);
		var sprite = property.FindPropertyRelative("sprite");
		var AtlasUsing = property.FindPropertyRelative("AtlasUsing");


		EditorGUI.PropertyField(new Rect(20, position.y+ElementSpacing*1, 300, ElementSpacing), sprite, GUIContent.none);
		EditorGUI.PropertyField(new Rect(20, position.y+ElementSpacing*2, 300, ElementSpacing), property.FindPropertyRelative("secondDelay"), GUIContent.none);
		EditorGUI.PropertyField(new Rect(20, position.y+ElementSpacing*3, 300, ElementSpacing), AtlasUsing, GUIContent.none);

		if (EditorGUI.EndChangeCheck())
		{
			var COOL = (Sprite) sprite.objectReferenceValue;
			AtlasUsing.intValue = (int) AddressableSpritesHandler.FindAtlasContaining(COOL);
			Logger.Log("ARR update");
			texProp.stringValue = COOL.name;
		}
		property.serializedObject.ApplyModifiedProperties();
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		float totalHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spriteName") )+ ElementSpacing*3;

		return totalHeight;
	}
}
