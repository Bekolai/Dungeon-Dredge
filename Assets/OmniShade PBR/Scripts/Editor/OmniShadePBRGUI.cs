//------------------------------------
//           OmniShade PBR
//     Copyright© 2025 OmniShade     
//------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_PIPELINE_HDRP
using UnityEditor.Rendering.HighDefinition;
#endif

/**
 * This class manages the GUI for the shader, automatically enabling/disabling keywords when values change.
 **/
#if UNITY_2021_1_OR_NEWER && UNITY_PIPELINE_HDRP
public class OmniShadePBRGUI : LightingShaderGraphGUI
#else
public class OmniShadePBRGUI : ShaderGUI
#endif
{
	// Shader keywords which are automatically enabled/disabled based on usage
	static List<(string keyword, string name, PropertyType type, Vector4 defaultValue)> props =
		new List<(string keyword, string name, PropertyType type, Vector4 defaultValue)>(){
		("METALLIC_MAP_ON", "_MetallicGlossMap", PropertyType.Texture, Vector4.one),
		("NORMAL_MAP_ON", "_BumpMap", PropertyType.Texture, Vector4.one),
		("NORMAL_MAP2_ON", "_BumpMap2", PropertyType.Texture, Vector4.one),
		("EMISSION_MAP_ON", "_EmissionMap", PropertyType.Texture, Vector4.one),
		("OCCLUSION_MAP_ON", "_OcclusionMap", PropertyType.Texture, Vector4.one),
		("REFLECTION_ON", "_ReflectionMap", PropertyType.Texture, Vector4.one),
		("DETAIL_MAP_ON", "_DetailMap", PropertyType.Texture, Vector4.one),
		("LAYER1_ON", "_Layer1", PropertyType.Texture, Vector4.one),
		("LAYER2_ON", "_Layer2", PropertyType.Texture, Vector4.one),
		("LAYER3_ON", "_Layer3", PropertyType.Texture, Vector4.one),
		("HEIGHT_COLORS_TEX_ON", "_HeightColorsTex", PropertyType.Texture, Vector4.one),
		("SHADOW_OVERLAY_ON", "_ShadowOverlayTex", PropertyType.Texture, Vector4.one),
	};

	readonly List<string> headers = new() {
		"Metallic",
		"Normal Map 1",
		"Normal Map 2",
		"Occlusion",
		"Emission",
		"Rim",
		"Reflection",
		"Vertex Colors",
		"Detail Map",
		"Layer 1",
		"Layer 2",
		"Layer 3",
		"Height Colors",
		"Shadow Overlay",
		"Plant Sway",
		"Camera Fade",
	};

	readonly List<string> stripDisplayNamePrefixes = new() {
		"Height Colors",
		"Shadow Overlay",
		"Plant Sway",
	};

	enum PropertyType
	{
		Float, Vector, Texture
	};

	enum ExpandType
	{
		All,
		Active,
		Collapse,
		Keep,
	};

	ExpandType forceExpand = ExpandType.Active;
	readonly Dictionary<string, bool> headerOpenDic = new();
	readonly Dictionary<MaterialProperty, int> propOrder = new();
	bool isRenderingFoldoutOpen = true;
	bool isTransparentShader = false;

	static readonly Dictionary<string, GUIContent> toolTipsCache = new();

#if UNITY_2021_1_OR_NEWER && UNITY_PIPELINE_HDRP
    protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] properties) 
#else
	public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
#endif
	{
		// Multi-selection
		var mat = materialEditor.target as Material;
		var mats = new List<Material>();
		if (mat != null)
			mats.Add(mat);
		foreach (var selected in Selection.objects)
		{
			if (selected.GetType() == typeof(Material))
			{
				var selectedMat = selected as Material;
				if (selectedMat != mat && selectedMat != null &&
					selectedMat.shader.name.Contains(OmniShadePBR.NAME))
					mats.Add(selectedMat);
			}
		}

		this.RenderGUI(materialEditor, properties);

		// Loop selected materials
		foreach (var material in mats)
		{
			this.AutoEnableShaderKeywords(material);
			if (material.shader.name.ToLower().Contains("transparent"))
				this.isTransparentShader = true;
		}
	}

	void AutoEnableShaderKeywords(Material mat)
	{
		ConfigureHDRPProperties(mat);

		foreach (var prop in OmniShadePBRGUI.props)
		{
			if (!mat.HasProperty(prop.name))
				continue;

			// Check if property value is being used (not set to default)
			bool isInUse = false;
			switch (prop.type)
			{
				case PropertyType.Float:
					isInUse = mat.GetFloat(prop.name) != prop.defaultValue.x;
					break;
				case PropertyType.Vector:
					isInUse = mat.GetVector(prop.name) != prop.defaultValue;
					break;
				case PropertyType.Texture:
					isInUse = mat.GetTexture(prop.name) != null;
					break;
				default:
					break;
			}

			// Enable or disable shader keyword
			if (isInUse)
			{
				if (!mat.IsKeywordEnabled(prop.keyword))
					mat.EnableKeyword(prop.keyword);
			}
			else if (mat.IsKeywordEnabled(prop.keyword))
				mat.DisableKeyword(prop.keyword);
		}

		this.AutoConfigureProperties(mat);
	}

	void AutoConfigureProperties(Material mat) {
		// Disable Reflection Mask With Rim if Reflection Disabled
		if (!mat.IsKeywordEnabled("REFLECTION_ON") &&
			mat.HasProperty("_ReflectionMaskWithRim") && mat.GetFloat("_ReflectionMaskWithRim") == 1)
			mat.SetFloat("_ReflectionMaskWithRim", 0);

		// Camera fade
		float fadeStart = mat.HasProperty("_CameraFadeStart") ? mat.GetFloat("_CameraFadeStart") : 0;
		float fadeEnd = mat.HasProperty("_CameraFadeEnd") ? mat.GetFloat("_CameraFadeEnd") : 0;
		bool cameraFadeEnabled = fadeStart < fadeEnd;
		EnableDisableKeyword(mat, cameraFadeEnabled, "CAMERA_FADE_ON");

#if !UNITY_PIPELINE_HDRP
		if (mat.HasProperty("_EmissionColor2")) {
			var emissionColor = mat.GetVector("_EmissionColor2");
			if ((Vector3)emissionColor != Vector3.zero)
				mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
			else
				mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
		}
#endif
	}

	void EnableDisableKeyword(Material mat, bool value, string keyword) {
		if (value) {
			if (!mat.IsKeywordEnabled(keyword))
				mat.EnableKeyword(keyword);
		}
		else {
			if (mat.IsKeywordEnabled(keyword))
				mat.DisableKeyword(keyword);
		}
	}

	public static void ConfigureHDRPProperties(Material mat) {
#if UNITY_PIPELINE_HDRP
		bool resetKeywords = false;
		if (mat.HasProperty("_AlphaCutoffEnable") && mat.GetFloat("_AlphaCutoffEnable") == 0) {
			mat.SetFloat("_AlphaCutoffEnable", 1);
			resetKeywords = true;
		}
		int isTransparent = mat.shader.name.Contains("Transparent") ? 1 : 0;
		if (mat.HasProperty("_SurfaceType") && mat.GetFloat("_SurfaceType") != isTransparent) {
			mat.SetFloat("_SurfaceType", isTransparent);
			resetKeywords = true;
		}
		
		if (resetKeywords)
			HDShaderUtils.ResetMaterialKeywords(mat);
#endif
	}

	void RenderGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
		materialEditor.SetDefaultGUIWidths();

		// Documentation button
		var content = new GUIContent(EditorGUIUtility.IconContent("_Help")) {
			text = OmniShadePBR.NAME + " Docs",
			tooltip = OmniShadePBR.DOCS_URL
		};
		if (GUILayout.Button(content))
			Help.BrowseURL(OmniShadePBR.DOCS_URL);

		// Expand/Close all buttons
		GUILayout.BeginHorizontal();
		var expandAll = new GUIContent(EditorGUIUtility.IconContent("Toolbar Plus")) { text = "Expand All" };
		if (GUILayout.Button(expandAll))
			this.forceExpand = ExpandType.All;
		if (GUILayout.Button("Expand Active"))
			this.forceExpand = ExpandType.Active;
		var closeAll = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus")) { text = "Collapse" };
		if (GUILayout.Button(closeAll))
			this.forceExpand = ExpandType.Collapse;
		GUILayout.EndHorizontal();

		this.RenderShaderProperties(materialEditor, properties);

		// Append Unity rendering options to last group
		this.isRenderingFoldoutOpen = this.BeginFoldout(this.isRenderingFoldoutOpen, "Rendering");
		if (this.isRenderingFoldoutOpen) {
#if !(UNITY_2021_1_OR_NEWER && UNITY_PIPELINE_HDRP)
	#if !UNITY_PIPELINE_HDRP
			materialEditor.RenderQueueField();
	#endif
			materialEditor.EnableInstancingField();
			materialEditor.DoubleSidedGIField();
#else
			uiBlocks.RemoveAll(b => b is ShaderGraphUIBlock);
			base.OnMaterialGUI(materialEditor, properties);
#endif
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}

	void RenderShaderProperties(MaterialEditor materialEditor, MaterialProperty[] properties) {
		this.RefreshPropertyHeaders(materialEditor);

		bool isFoldoutOpen = true;
		var headersVisited = new List<string>();
		this.SortProperties(properties);
		foreach (var prop in properties) {
			// Skip Unity vars
			if (prop.displayName.StartsWith("_") || prop.displayName.StartsWith("unity_"))
				continue;
			// Skip hidden vars
			if (prop.propertyFlags == ShaderPropertyFlags.HideInInspector)
				continue;
			// Skip all caps
			if (this.IsAllUpper(prop.displayName))
				continue;
			// Skip camera fade on non-transparent shader
			if (!this.isTransparentShader && prop.displayName.StartsWith("Camera Fade"))
				continue;
				
			// Check if new header
			string headerName = this.GetHeaderName(prop.displayName);
			if (!string.IsNullOrEmpty(headerName) && !headersVisited.Contains(headerName)) {
				headersVisited.Add(headerName);
				bool isOpen = this.headerOpenDic[headerName];
				EditorGUILayout.EndFoldoutHeaderGroup();

				// Begin foldout header
				this.headerOpenDic[headerName] = isFoldoutOpen = this.BeginFoldout(isOpen, headerName);
			}

			// Render shader property
			if (isFoldoutOpen)
				this.RenderShaderProperty(materialEditor, prop);
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}

	void RenderShaderProperty(MaterialEditor materialEditor, MaterialProperty prop) {
		var label = this.GetDisplayLabel(prop.displayName);

		// Show tiling/offset for textures
		if (prop.propertyType == ShaderPropertyType.Texture && label != "Reflection Map")
			materialEditor.TextureProperty(prop, label, true);
		else {
			var content = this.GetTooltip(label);
			materialEditor.ShaderProperty(prop, content);
		}
	}

	string GetDisplayLabel(string displayName) {
		var label = displayName;
		foreach (string prefix in stripDisplayNamePrefixes) {
			if (label.StartsWith(prefix))
				label = label.Replace(prefix + " ", string.Empty);
		}

		if (label == "Metallic Smoothness")
			label = "Smoothness";
		else if (label.StartsWith("Detail Map Use"))
			label = label.Replace("Detail Map ", string.Empty);
		else if (label.Contains("Mask With"))
			label = label.Substring(label.IndexOf("Mask"));
		else
			label = label.Replace("Rendering ", string.Empty);
		return label;
	}

	void RefreshPropertyHeaders(MaterialEditor materialEditor) {
		var mat = materialEditor.target as Material;
		var shader = mat.shader;

		// Initialize to all close if keep active
		if (this.forceExpand == ExpandType.Active) {
			var headers = new List<string>(this.headerOpenDic.Keys);
			foreach (var header in headers)
				this.headerOpenDic[header] = false;
		}

		int numProps = shader.GetPropertyCount();
		var newHeaders = new List<string>();
		for (int i = 0; i < numProps; i++) {
			// Skip if not part of a header group
			string propDesc = shader.GetPropertyDescription(i);
			var propType = shader.GetPropertyType(i);
			string headerName = this.GetHeaderName(propDesc);

			if (string.IsNullOrEmpty(headerName))
				continue;

			// Skip if not a key property that can toggle a group
			if (!this.IsKeyProperty(propDesc, propType))
				continue;

			newHeaders.Add(headerName);

			// Check if property active
			bool isInUse = false;
			switch (this.forceExpand) {
				case ExpandType.All:
					isInUse = true;
					this.isRenderingFoldoutOpen = true;
					break;
				case ExpandType.Active:
					string propName = shader.GetPropertyName(i);
					isInUse = this.IsPropertyActive(mat, propName, propDesc, propType);
					this.isRenderingFoldoutOpen = true;
					break;
				case ExpandType.Collapse:
					isInUse = false;
					this.isRenderingFoldoutOpen = false;
					break;
				default: 
					break;
			}

			// Cache
			if (!this.headerOpenDic.ContainsKey(headerName))
				this.headerOpenDic.Add(headerName, isInUse);
			if (this.forceExpand == 0 || this.forceExpand == ExpandType.Active)
				this.headerOpenDic[headerName] |= isInUse;
			else if (this.forceExpand == ExpandType.Collapse)
				this.headerOpenDic[headerName] = isInUse;
		}
		this.forceExpand = ExpandType.Keep;

		// Remove any headers that were deleted
		var deletedHeaders = this.headerOpenDic.Keys.Except(newHeaders);
		foreach (var deletedHeader in deletedHeaders)
			this.headerOpenDic.Remove(deletedHeader);
	}

	bool IsKeyProperty(string propDesc, ShaderPropertyType propType) {
		var keyProps = new List<string>() {
			"Metallic",
			"Emission Color",
			"Camera Fade Start Distance",
		};

		return (propType == ShaderPropertyType.Float && propDesc.StartsWith("Enable")) ||
			propType == ShaderPropertyType.Texture || keyProps.Contains(propDesc);
	}

	bool IsPropertyActive(Material mat, string propName, string propDesc, ShaderPropertyType propType) {
		if (!mat.HasProperty(propName))
			return false;

		if ((propType == ShaderPropertyType.Float && propDesc.StartsWith("Enable") && mat.GetFloat(propName) == 1) ||
			(propType == ShaderPropertyType.Texture && mat.GetTexture(propName) != null) ||
			propDesc == "Metallic" ||
			(propDesc == "Emission Color" && (Vector3)mat.GetVector(propName) != Vector3.zero) ||
			(propName == "_CameraFadeStart" && mat.IsKeywordEnabled("CAMERA_FADE_ON")))
			return true;
		return false;
	}

	void SortProperties(MaterialProperty[] properties) {
		// Preserve original order
		this.propOrder.Clear();
		for (int i = 0; i < properties.Length; i++)
			this.propOrder.Add(properties[i], i);
		Array.Sort(properties, PropertyComparer);
	}

	int PropertyComparer(MaterialProperty a, MaterialProperty b) {
		// Sort in order of not grouped, groups by order of header, keyword or not
		int headerA = -1;
		int headerB = -1;
		for (int i = 0; i < this.headers.Count; i++) {
			string header = this.headers[i];
			if (a.displayName.Contains(header))
				headerA = i;
			if (b.displayName.Contains(header))
				headerB = i;
		}
		if (headerA != headerB)
			return headerA < headerB ? -1 : 1;
		else {
			bool keywordA = a.displayName.Contains("Enable");
			bool keywordB = b.displayName.Contains("Enable");
			if (keywordA && !keywordB)
				return -1;
			else if (!keywordA && keywordB)
				return 1;
			else {  // Preserve original order
				if (this.propOrder[a] < this.propOrder[b])
					return -1;
				else if (this.propOrder[a] > this.propOrder[b])
					return 1;
				else
					return 0;
			}
		}
	}

	string GetHeaderName(string propDesc) {
		foreach (var header in this.headers) {
			if (propDesc.Contains(header))
				return header;
		}
		return string.Empty;
	}

	bool BeginFoldout(bool isOpen, string name) {
		var content = this.GetTooltip(name);
		var defaultColor = GUI.backgroundColor;
		GUI.backgroundColor = new Color(1.35f, 1.35f, 1.35f);
		bool retVal = EditorGUILayout.BeginFoldoutHeaderGroup(isOpen, content);
		GUI.backgroundColor = defaultColor;
		return retVal;
	}

	GUIContent GetTooltip(string label) {
		// Check cache first
		if (OmniShadePBRGUI.toolTipsCache.ContainsKey(label))
			return OmniShadePBRGUI.toolTipsCache[label];

		string tooltip;
		switch (label) {
			case "Ignore Main Texture Alpha": tooltip = "Ignore the alpha channel on the texture, forcing it to be opaque."; break;
			case "Rim Direction": tooltip = "Rim light direction in world space."; break;
			case "Mask With Rim": tooltip = "Mask the reflection with the rim's fresnel effect to simulate reflections only at glancing angles."; break;
			case "Use Alpha Blend Instead Of Multiply": tooltip = "Blend using the alpha value instead of multiplying the detail map."; break;
			case "Mask With Vertex Color (A)": tooltip = "Mask with the vertex color's alpha channel."; break;
			case "Mask With Vertex Color (R)": tooltip = "Mask with the vertex color's red channel."; break;
			case "Mask With Vertex Color (G)": tooltip = "Mask with the vertex color's green channel."; break;
			case "Mask With Vertex Color (B)": tooltip = "Mask with the vertex color's blue channel."; break;
			case "Height Colors": tooltip = "Apply a color to the object based on its local or world-space height."; break;
			case "Enable Height Colors": tooltip = "Apply a color to the object based on its local or world-space height."; break;
			case "Use Local Space Instead Of World Space": tooltip = "Use local space instead of world space for height calculation."; break;
			default: tooltip = ""; break;
		}

		// Create tool tip and cache
		var content = new GUIContent() {
			text = label,
			tooltip = tooltip
		};
		OmniShadePBRGUI.toolTipsCache.Add(label, content);
		return content;
	}

	bool IsAllUpper(string str) {
		foreach (char c in str) {
			if (Char.IsLetter(c) && !Char.IsUpper(c))
				return false;
		}
		return true;
	}

	public override void AssignNewShaderToMaterial(Material mat, Shader oldShader, Shader newShader) {
		// Convert texture mapping
		var textureMapping = new Dictionary<string, string>() {
			{ "_MainTex", "_BaseMap" },
			{ "_BaseColorMap", "_BaseMap" },
			{ "_EmissiveColorMap", "_EmissionMap" },
			{ "_MaskMap", "_MetallicGlossMap" },
			{ "_NormalMap", "_BumpMap" },
			{ "_OcclusionMap", "_OcclusionMap" },
		};
		var tilingOffsetMapping = new Dictionary<string, Vector4>();

		// Fetch textures from mapping
		var texToReplace = new Dictionary<string, Texture>();
		foreach (var texMap in textureMapping) {
			if (mat.HasProperty(texMap.Key) && mat.GetTexture(texMap.Key) != null) {
				if (!texToReplace.ContainsKey(texMap.Value)) {
					texToReplace.Add(texMap.Value, mat.GetTexture(texMap.Key));

					// Store tiling offset
					Vector4 tilingOffset;
					Vector2 tiling, offset;
					tiling = mat.GetTextureScale(texMap.Key);
					offset = mat.GetTextureOffset(texMap.Key);
					tilingOffset.x = tiling.x;
					tilingOffset.y = tiling.y;
					tilingOffset.z = offset.x;
					tilingOffset.w = offset.y;
					tilingOffsetMapping.Add(texMap.Value, tilingOffset);
				}
				mat.SetTexture(texMap.Key, null);
			}
		}
		// Get specific properties           
		Vector4 baseColor = Vector4.one;
		Vector4 emissionColor = Vector4.zero;
		float smoothness = 0.5f, bumpScale = 1.0f;
		if (mat.HasProperty("_BaseColor"))
			baseColor = mat.GetVector("_BaseColor");
		else if (mat.HasProperty("_Color"))
			baseColor = mat.GetVector("_Color");
		if (mat.HasProperty("_Smoothness"))
			smoothness = mat.GetFloat("_Smoothness");
		if (mat.HasProperty("_NormalScale"))
			bumpScale = mat.GetFloat("_NormalScale");
		else if (mat.HasProperty("_BumpScale"))
			bumpScale = mat.GetFloat("_BumpScale");
		if (mat.HasProperty("_EmissiveColor"))
			emissionColor = mat.GetVector("_EmissiveColor");
		else if (mat.HasProperty("_EmissionColor2"))
			emissionColor = mat.GetVector("_EmissionColor2");
		else if (mat.HasProperty("_EmissionColor") && mat.globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack)
			emissionColor = mat.GetVector("_EmissionColor");

		// Replace shader
		base.AssignNewShaderToMaterial(mat, oldShader, newShader);

		// Replace textures
		Vector2 baseTiling = Vector2.one, baseOffset = Vector2.zero;
		foreach (var texToRep in texToReplace) {
			var texName = texToRep.Key;
			if (mat.HasProperty(texName)) {
				mat.SetTexture(texName, texToRep.Value);
				if (tilingOffsetMapping.ContainsKey(texName)) {
					// Restore tiling offset
					Vector4 tilingOffset = tilingOffsetMapping[texName];
					Vector2 tiling = (Vector2)tilingOffset;
					Vector2 offset;
					offset.x = tilingOffset.z;
					offset.y = tilingOffset.w;
					mat.SetTextureScale(texName, tiling);
					mat.SetTextureOffset(texName, offset);

					// Store base tiling offset
					if (texName == "_BaseMap") {
						baseTiling = tiling;
						baseOffset = offset;
					}
				}
			}
		}

		// If prev shader is Lit, apply base tiling offset to all tex
		if (oldShader.name.Contains("Lit") || oldShader.name == "Standard") {
			foreach (var texMap in textureMapping) {
				var texName = texMap.Value;
				if (mat.HasProperty(texName)) {
					mat.SetTextureScale(texName, baseTiling);
					mat.SetTextureOffset(texName, baseOffset);
				}
			}
		}

		// Replace properties
		mat.SetVector("_BaseColor", baseColor);
		mat.SetVector("_EmissionColor2", emissionColor);
		mat.SetFloat("_Smoothness", smoothness);
		mat.SetFloat("_BumpScale", bumpScale);
		if ((Vector3)emissionColor != Vector3.zero)
			mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

		this.forceExpand = ExpandType.Active;
	}
}
