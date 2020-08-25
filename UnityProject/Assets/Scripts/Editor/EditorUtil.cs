using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class EditorUtil
{
	public static Type TupleTypeReference = Type.GetType("System.ITuple, mscorlib");
	public static HashSet<T> GetAttributes<T>(object Script, int Depth = 0, HashSet<T> toAddto = null ) where T : class
	{
		var ToReturn  = new HashSet<T>();
		if (toAddto != null)
		{
			ToReturn = toAddto;
		}

		Depth++;
		//if (Depth <= 10)
		//{
		//Logger.Log("1");
		Type monoType = Script.GetType();
		foreach (FieldInfo Field in monoType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
		{
			//Logger.Log("2");
			if (Field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				//Logger.Log("3 " + Field.FieldType);
				if (Field.FieldType == typeof(SpriteSheetAndData))
				{
					//Logger.Log("4");
					(Field.GetValue(Script) as SpriteSheetAndData).setSprites();
				}
				//Logger.Log("5");

				ReflectionSpriteSheetAndData(Field.FieldType, Script, Info: Field, Depth: Depth, toAddto:ToReturn);
			}
		}
		if (TupleTypeReference == monoType) //Causes an error if this is not here and Tuples can not get Custom properties so it is I needed to get the properties
		{
			foreach (PropertyInfo Properties in monoType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (Properties.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					if (Properties.PropertyType == typeof(SpriteSheetAndData))
					{
						(Properties.GetValue(Script) as SpriteSheetAndData).setSprites();
					}
					ReflectionSpriteSheetAndData(Properties.PropertyType, Script, PInfo: Properties, Depth: Depth, toAddto:ToReturn);
				}
			}
		}

		return ToReturn;
	}
	public static void ReflectionSpriteSheetAndData<T>(Type VariableType, object Script, FieldInfo Info = null, PropertyInfo PInfo = null, int Depth = 0, HashSet<T> toAddto = null) where T : class
	{
		if (Info == null && PInfo == null)
		{
			if (VariableType == typeof(T))
			{
				toAddto.Add(Script as T);
			}

			foreach (FieldInfo method in VariableType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
			{
				if (method.FieldType == typeof(T))
				{
					toAddto.Add(method.GetValue(Script) as T);
				}

				if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					if (method.FieldType.IsGenericType)
					{
						IEnumerable list = method.GetValue(Script) as IEnumerable;
						if (list != null)
						{
							foreach (var c in list)
							{
								Type valueType = c.GetType();
								ReflectionSpriteSheetAndData<T>(c.GetType(), c);

							}
						}
					}
					else if (VariableType.IsClass && VariableType != typeof(string))
					{
						if (method.GetValue(Script) != null)
						{
							//Logger.Log(method.ToString());
							GetAttributes<T>(method.GetValue(Script), Depth);
						}
					}
				}
			}
		}
		else {
			if (Info == null)
			{
				if (PInfo.PropertyType == typeof(T))
				{
					toAddto.Add(PInfo.GetValue(Script)  as T);
				}
			}
			else
			{
				if (Info.FieldType == typeof(T))
				{
					toAddto.Add(Info.GetValue(Script)  as T);
				}
			}


			if (VariableType.IsGenericType)
			{
				IEnumerable list;
				Type TType;
				if (Info == null)
				{
					list = PInfo.GetValue(Script) as IEnumerable;
					TType = PInfo.PropertyType;
				}
				else
				{
					list = Info.GetValue(Script) as IEnumerable;
					TType = Info.FieldType;
				}
				if (list != null)
				{
					foreach (object c in list)
					{
						Type valueType = c.GetType();
						ReflectionSpriteSheetAndData<T>(c.GetType(), c);
					}
				}
			}
			else if (VariableType.IsClass && VariableType != typeof(string))
			{
				if (Info == null)
				{
					if (PInfo.GetValue(Script) != null)
					{
						GetAttributes<T>(PInfo.GetValue(Script), Depth);
					}
				}
				else
				{
					if (Info.GetValue(Script) != null)
					{
						GetAttributes<T>(Info.GetValue(Script), Depth);
					}
				}
			}
		}
	}
}
