using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesPanel : MonoBehaviour
{
	public Prototype FreeFloatField;
	public Prototype RangedFloatField;
	public Prototype EnumField;
	public Prototype BoolField;
	public Prototype ColorField;
	public Prototype StringField; // A beautiful town, home of the Stringsons
	public Prototype Divider;

	public int ResolutionExponentMinumum = 6;
	public int ResolutionExponentCount = 5;
	
	private readonly List<Prototype> _brushPropertyFields = new List<Prototype>();
	private event EventHandler RefreshPropertyValues;

	public void Clear()
	{
		foreach (var property in _brushPropertyFields)
		{
			property.ReturnToPool();
		}
		
		RefreshPropertyValues = null;
	}

	// public void Inspect(ProceduralMaterial substance)
	// {
	// 	foreach (var property in substance.GetProceduralPropertyDescriptions())
	// 	{
	// 		switch (property.type)
	// 		{
	// 			case ProceduralPropertyType.Boolean:
	// 				Inspect(property.label, () => substance.GetProceduralBoolean(property.name),
	// 					b =>
	// 					{
	// 						substance.SetProceduralBoolean(property.name, b);
	// 						substance.RebuildTextures();
	// 					});
	// 				break;
	// 			case ProceduralPropertyType.Float:
	// 				if (property.hasRange)
	// 					Inspect(property.label,
	// 						() => substance.GetProceduralFloat(property.name),
	// 						f =>
	// 						{
	// 							substance.SetProceduralFloat(property.name, f);
	// 							substance.RebuildTextures();
	// 						},
	// 						property.minimum,
	// 						property.maximum);
	// 				else
	// 					Inspect(property.label,
	// 						() => substance.GetProceduralFloat(property.name),
	// 						f =>
	// 						{
	// 							substance.SetProceduralFloat(property.name, f);
	// 							substance.RebuildTextures();
	// 						});
	// 				break;
	// 			case ProceduralPropertyType.Vector2:
	// 				break;
	// 			case ProceduralPropertyType.Vector3:
	// 				break;
	// 			case ProceduralPropertyType.Vector4:
	// 				break;
	// 			case ProceduralPropertyType.Color3:
	// 				Inspect(property.label,
	// 					() =>
	// 					{
	// 						var c = substance.GetProceduralColor(property.name);
	// 						c.a = 1.0f;
	// 						return c;
	// 					},
	// 					c =>
	// 					{
	// 						substance.SetProceduralColor(property.name, c);
	// 						substance.RebuildTextures();
	// 					});
	// 				break;
	// 			case ProceduralPropertyType.Color4:
	// 				Inspect(property.label,
	// 					() => substance.GetProceduralColor(property.name),
	// 					c =>
	// 					{
	// 						substance.SetProceduralColor(property.name, c);
	// 						substance.RebuildTextures();
	// 					});
	// 				break;
	// 			case ProceduralPropertyType.Enum:
	// 				Inspect(property.label,
	// 					() => substance.GetProceduralEnum(property.name),
	// 					i => substance.SetProceduralEnum(property.name, i),
	// 					property.enumOptions);
	// 				break;
	// 			case ProceduralPropertyType.Texture:
	// 				break;
	// 			default:
	// 				throw new ArgumentOutOfRangeException();
	// 		}
	// 	}
	// 	
	// 	RefreshValues();
	// }

	public void Inspect(object obj, bool inspectablesOnly = false)
	{
		foreach (var field in obj.GetType().GetFields())
			Inspect(obj, field, inspectablesOnly);
		
		RefreshValues();
	}

	public void Inspect(object obj, FieldInfo field, bool inspectablesOnly = false)
	{
		var inspectable = field.GetCustomAttribute<InspectableAttribute>();
		if (inspectable == null) return;
		
		if (field.FieldType == typeof(float))
		{
			if (inspectable is RangedFloatInspectableAttribute && RangedFloatField!=null)
			{
				var range = inspectable as RangedFloatInspectableAttribute;
				Inspect(field.Name, () => (float) field.GetValue(obj), f => field.SetValue(obj, f), range.Min, range.Max);
			}
			else if(FreeFloatField!=null)
				Inspect(field.Name, () => (float) field.GetValue(obj), f => field.SetValue(obj, f));
		}
		else if (field.FieldType.IsEnum && EnumField!=null) Inspect(field.Name, () => (int) field.GetValue(obj), i => field.SetValue(obj, i), Enum.GetNames(field.FieldType));
		else if (field.FieldType == typeof(Color) && ColorField!=null) Inspect(field.Name, () => (Color) field.GetValue(obj), c => field.SetValue(obj, c));
		else if (field.FieldType == typeof(bool) && BoolField!=null) Inspect(field.Name, () => (bool) field.GetValue(obj), b => field.SetValue(obj, b));
		else Debug.Log($"Field \"{field.Name}\" has unknown type {field.FieldType.Name}. No inspector was generated.");
	}
	
	public void Inspect(string name, Func<string> read, Action<string> write)
	{
		var stringFieldInstance = StringField.Instantiate<Prototype>();
		_brushPropertyFields.Add(stringFieldInstance);
		stringFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var inputField = stringFieldInstance.GetComponentInChildren<TMP_InputField>();
		inputField.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => inputField.text = read();
	}

	public void Inspect(string name, Func<float> read, Action<float> write)
	{
		var freeFloatFieldInstance = FreeFloatField.Instantiate<Prototype>();
		_brushPropertyFields.Add(freeFloatFieldInstance);
		freeFloatFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var inputField = freeFloatFieldInstance.GetComponentInChildren<TMP_InputField>();
		inputField.onValueChanged.AddListener(val => write(float.Parse(val)));
		RefreshPropertyValues += (sender, args) => inputField.text = read().ToString(CultureInfo.InvariantCulture);
	}
	
	public void Inspect(string name, Func<float> read, Action<float> write, float min, float max)
	{
		var rangedFloatFieldInstance = RangedFloatField.Instantiate<Prototype>();
		_brushPropertyFields.Add(rangedFloatFieldInstance);
		rangedFloatFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var slider = rangedFloatFieldInstance.GetComponentInChildren<Slider>();
		slider.minValue = min;
		slider.maxValue = max;
		slider.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => slider.value = read();
	}
	
	public void Inspect(string name, Func<Color> read, Action<Color> write)
	{
		var colorFieldInstance = ColorField.Instantiate<Prototype>();
		_brushPropertyFields.Add(colorFieldInstance);
		colorFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var colorButton = colorFieldInstance.GetComponentInChildren<ColorButton>();
		colorButton.OnColorChanged.AddListener(col => write(col));
		RefreshPropertyValues += (sender, args) => colorButton.GetComponent<Image>().color = read();
	}
	
	public void Inspect(string name, Func<bool> read, Action<bool> write)
	{
		var boolFieldInstance = BoolField.Instantiate<Prototype>();
		_brushPropertyFields.Add(boolFieldInstance);
		boolFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var toggle = boolFieldInstance.GetComponentInChildren<Toggle>();
		toggle.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => toggle.isOn = read();
	}
	
	public void Inspect(string name, Func<int> read, Action<int> write, string[] enumOptions)
	{
		var enumFieldInstance = EnumField.Instantiate<Prototype>();
		_brushPropertyFields.Add(enumFieldInstance);
		enumFieldInstance.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = name;
		var dropDown = enumFieldInstance.GetComponentInChildren<TMP_Dropdown>();
		dropDown.options = enumOptions.Select(s => new TMP_Dropdown.OptionData(s)).ToList();
		dropDown.onValueChanged.AddListener(val => write(val));
		RefreshPropertyValues += (sender, args) => dropDown.value = read();
	}
/*
	public void RefreshBrushProperties()
	{
		var brush = BrushManager.Instance.SelectedLayer.LayerBrush;
		
		foreach (var brushProperty in _brushPropertyFields)
			Destroy(brushProperty.gameObject);
		
		_brushPropertyFields.Clear();
		
		var depthInputField = DepthPropertyRect.GetComponentInChildren<TMP_InputField>();
		depthInputField.text = brush.Depth.ToString(CultureInfo.InvariantCulture);
		depthInputField.onValueChanged.RemoveAllListeners();
		depthInputField.onValueChanged.AddListener(val => brush.Depth = float.Parse(val));
		
		var opacitySlider = OpacityPropertyRect.GetComponentInChildren<Slider>();
		opacitySlider.minValue = 0;
		opacitySlider.maxValue = 0;
		opacitySlider.value = brush.Opacity;
		opacitySlider.onValueChanged.RemoveAllListeners();
		opacitySlider.onValueChanged.AddListener(val => brush.Opacity = val);
		
		var softnessSlider = SoftnessPropertyRect.GetComponentInChildren<Slider>();
		softnessSlider.minValue = .05f;
		softnessSlider.maxValue = 5;
		softnessSlider.value = brush.Softness;
		softnessSlider.onValueChanged.RemoveAllListeners();
		softnessSlider.onValueChanged.AddListener(val => brush.Softness = val);

		var shaderBrush = brush as BrushManager.ShaderBrush;
		if (shaderBrush != null)
		{
			if (shaderBrush.BrushMaterial.HasProperty("_Tiling"))
			{
				TilingPropertyRect.gameObject.SetActive(true);
				Debug.Log("Tiling property found. Enabling inspector.");
				
				var inputField = TilingPropertyRect.GetComponentInChildren<TMP_InputField>();
				inputField.text = shaderBrush.Tiling.ToString(CultureInfo.InvariantCulture);
				inputField.onValueChanged.RemoveAllListeners();
				inputField.onValueChanged.AddListener(val => shaderBrush.Tiling = float.Parse(val));
			}
			else
			{
				Debug.Log("Tiling property not found. Disabling inspector.");
				TilingPropertyRect.gameObject.SetActive(false);
			}
			
			foreach (var inspectedShaderProperty in BrushManager.Instance.InspectedShaderProperties.Where(sp =>
				shaderBrush.DisplacementMaterial.HasProperty(sp)))
			{
				var fieldInstance = Instantiate(FreeFloatFieldPrefab, ContentRect);
				_brushPropertyFields.Add(fieldInstance);
				fieldInstance.Find("Label").GetComponent<TextMeshProUGUI>().text = SplitCamelCase(inspectedShaderProperty.Trim('_'));
				fieldInstance.GetComponentInChildren<TMP_InputField>().onValueChanged
					.AddListener(val => shaderBrush.DisplacementMaterial.SetFloat(inspectedShaderProperty, float.Parse(val)));
			}
		}

		var substanceBrush = brush as BrushManager.SubstanceBrush;
		if (substanceBrush != null)
		{
			var tilingInputField = TilingPropertyRect.GetComponentInChildren<TMP_InputField>();
			tilingInputField.text = substanceBrush.Tiling.ToString(CultureInfo.InvariantCulture);
			tilingInputField.onValueChanged.RemoveAllListeners();
			tilingInputField.onValueChanged.AddListener(val => substanceBrush.Tiling = float.Parse(val));
			
			var resolutionField = Instantiate(EnumFieldPrefab, ContentRect);
			_brushPropertyFields.Add(resolutionField);
			resolutionField.Find("Label").GetComponent<TextMeshProUGUI>().text = "Resolution";
			
			var resolutionDropdown = resolutionField.GetComponentInChildren<TMP_Dropdown>();
			Func<int, int> intPow = i => Mathf.ClosestPowerOfTwo((int) Mathf.Pow(2, i + SubstanceResolutionExponentMinumum));
			resolutionDropdown.options =
				Enumerable.Range(0, SubstanceResolutionExponentCount).Select(i => intPow(i))
					.Select(pow => new TMP_Dropdown.OptionData($"{pow}x{pow}")).ToList();
			resolutionDropdown.onValueChanged.AddListener(i => Debug.Log($"Selected res = {intPow(i)}"));

			Debug.Log(substanceBrush.Substance.GetProceduralPropertyDescriptions().Aggregate("Substance Properties:",
				(s, desc) => $"{s}\n\"{desc.name}\": {Enum.GetName(typeof(ProceduralPropertyType), desc.type)}"));

			foreach (var proceduralPropertyDescription in substanceBrush.Substance.GetProceduralPropertyDescriptions()
				.Where(ppd => !BrushManager.Instance.IgnoreSubstanceProperties.Any(isp => isp.Equals(ppd.name))))
			{
				switch (proceduralPropertyDescription.type)
				{
					case ProceduralPropertyType.Boolean:
						var boolFieldInstance = Instantiate(BoolFieldPrefab, ContentRect);
						_brushPropertyFields.Add(boolFieldInstance);
						boolFieldInstance.Find("Label").GetComponent<TextMeshProUGUI>().text = proceduralPropertyDescription.label;
						var toggle = boolFieldInstance.GetComponentInChildren<Toggle>();
						toggle.isOn = substanceBrush.Substance.GetProceduralBoolean(proceduralPropertyDescription.name);
						toggle.onValueChanged.AddListener(val =>
						{
							substanceBrush.Substance.SetProceduralBoolean(proceduralPropertyDescription.name, val);
							substanceBrush.Substance.RebuildTextures();
						});
						break;
						
					case ProceduralPropertyType.Float:
						if (proceduralPropertyDescription.hasRange)
						{
							var rangedFloatFieldInstance = Instantiate(RangedFloatFieldPrefab, ContentRect);
							_brushPropertyFields.Add(rangedFloatFieldInstance);
							rangedFloatFieldInstance.Find("Label").GetComponent<TextMeshProUGUI>().text = proceduralPropertyDescription.label;
							var slider = rangedFloatFieldInstance.GetComponentInChildren<Slider>();
							slider.minValue = proceduralPropertyDescription.minimum;
							slider.maxValue = proceduralPropertyDescription.maximum;
							slider.value = substanceBrush.Substance.GetProceduralFloat(proceduralPropertyDescription.name);
							slider.onValueChanged.AddListener(val =>
							{
								substanceBrush.Substance.SetProceduralFloat(proceduralPropertyDescription.name,val);
								substanceBrush.Substance.RebuildTextures();
							});
						}
						else
						{
							var freeFloatFieldInstance = Instantiate(FreeFloatFieldPrefab, ContentRect);
							_brushPropertyFields.Add(freeFloatFieldInstance);
							freeFloatFieldInstance.Find("Label").GetComponent<TextMeshProUGUI>().text = proceduralPropertyDescription.label;
							var inputField = freeFloatFieldInstance.GetComponentInChildren<TMP_InputField>();
							inputField.text = substanceBrush.Substance.GetProceduralFloat(proceduralPropertyDescription.name).ToString(CultureInfo.InvariantCulture);
							inputField.onValueChanged.AddListener(val =>
							{
								substanceBrush.Substance.SetProceduralFloat(proceduralPropertyDescription.name,float.Parse(val));
								substanceBrush.Substance.RebuildTextures();
							});
						}
						break;
						
					case ProceduralPropertyType.Vector2:
						break;
					case ProceduralPropertyType.Vector3:
						break;
					case ProceduralPropertyType.Vector4:
						break;
						
					case ProceduralPropertyType.Color3:
						var colorFieldInstance = Instantiate(ColorFieldPrefab, ContentRect);
						_brushPropertyFields.Add(colorFieldInstance);
						colorFieldInstance.Find("Label").GetComponent<TextMeshProUGUI>().text = proceduralPropertyDescription.label;
						var colorButton = colorFieldInstance.GetComponentInChildren<ColorButton>();
						var color3 = substanceBrush.Substance.GetProceduralColor(proceduralPropertyDescription.name);
						color3.a = 1.0f;
						colorButton.GetComponent<Image>().color = color3;
						colorButton.OnColorChanged.AddListener(col =>
						{
							substanceBrush.Substance.SetProceduralColor(proceduralPropertyDescription.name, col);
							substanceBrush.Substance.RebuildTextures();
						});
						break;
						
					case ProceduralPropertyType.Color4:
						var color4FieldInstance = Instantiate(ColorFieldPrefab, ContentRect);
						_brushPropertyFields.Add(color4FieldInstance);
						color4FieldInstance.Find("Label").GetComponent<TextMeshProUGUI>().text = proceduralPropertyDescription.label;
						var color4Button = color4FieldInstance.GetComponentInChildren<ColorButton>();
						color4Button.GetComponent<Image>().color = substanceBrush.Substance.GetProceduralColor(proceduralPropertyDescription.name);
						color4Button.OnColorChanged.AddListener(col =>
						{
							substanceBrush.Substance.SetProceduralColor(proceduralPropertyDescription.name, col);
							substanceBrush.Substance.RebuildTextures();
						});
						break;
						
					case ProceduralPropertyType.Enum:
						var enumFieldInstance = Instantiate(EnumFieldPrefab, ContentRect);
						_brushPropertyFields.Add(enumFieldInstance);
						enumFieldInstance.Find("Label").GetComponent<TextMeshProUGUI>().text = proceduralPropertyDescription.label;
						var dropDown = enumFieldInstance.GetComponentInChildren<TMP_Dropdown>();
						dropDown.options = proceduralPropertyDescription.enumOptions.Select(s => new TMP_Dropdown.OptionData(s)).ToList();
						dropDown.value = substanceBrush.Substance.GetProceduralEnum(proceduralPropertyDescription.name);
						dropDown.onValueChanged.AddListener(val =>
						{
							substanceBrush.Substance.SetProceduralEnum(proceduralPropertyDescription.name,val);
							substanceBrush.Substance.RebuildTextures();
						});
						break;
						
					case ProceduralPropertyType.Texture:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}*/
	
	public static string SplitCamelCase( string str )
	{
		return Regex.Replace( 
			Regex.Replace( 
				str, 
				@"(\P{Ll})(\P{Ll}\p{Ll})", 
				"$1 $2" 
			), 
			@"(\p{Ll})(\P{Ll})", 
			"$1 $2" 
		);
	}

	// Use this for initialization
	void Start ()
	{
		/*
		var brushTypeDropdown = BrushTypePropertyRect.GetComponentInChildren<TMP_Dropdown>();
		brushTypeDropdown.options = new List<TMP_Dropdown.OptionData>
		{
			new TMP_Dropdown.OptionData("Shader"),
			new TMP_Dropdown.OptionData("Substance")
		};
		brushTypeDropdown.onValueChanged.AddListener(brushType =>
		{
			if (brushType == 0)
				BrushManager.Instance.SelectedLayer.LayerBrush = new BrushManager.ShaderBrush(BrushManager.Instance.BrushShaders.First());
			else if (brushType == 1)
				BrushManager.Instance.SelectedLayer.LayerBrush = new BrushManager.SubstanceBrush(BrushManager.Instance.DefaultSubstance);
			RefreshBrushProperties();
		});

		var sizeInputField = SizePropertyRect.GetComponentInChildren<TMP_InputField>();
		sizeInputField.text = BrushManager.Instance.SplatSize.ToString(CultureInfo.InvariantCulture);
		sizeInputField.onValueChanged.AddListener(str => BrushManager.Instance.SplatSize = float.Parse(str));
		*/
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void RefreshValues()
	{
		RefreshPropertyValues?.Invoke(this, EventArgs.Empty);
	}
}

[AttributeUsage(AttributeTargets.Field)]
public class InspectableAttribute : Attribute
{
}

public class RangedFloatInspectableAttribute : InspectableAttribute
{
	public readonly float Min, Max;

	public RangedFloatInspectableAttribute(float min, float max)
	{
		Min = min;
		Max = max;
	}
}
