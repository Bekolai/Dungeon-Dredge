//------------------------------------
//           OmniShade PBR
//     Copyright© 2025 OmniShade     
//------------------------------------

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
#endif

/**
 * This class contains shader constants and utility functions.
 **/
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class OmniShadePBR {
	public const string NAME = "OmniShade PBR";
	public const string DOCS_URL = "https://pbr.omnishade.io";
	
#if UNITY_EDITOR
	const string SHADER_VARIANT_LIMIT_KEY = "UnityEditor.ShaderGraph.VariantLimit";
	const string SHADER_GRAPH_SETTINGS_FILE = "ProjectSettings/ShaderGraphSettings.asset";
	const int SHADER_VARIANT_LIMIT = 100000;

	static OmniShadePBR() {
		IncreaseShaderVariantLimit();
	}

	static void IncreaseShaderVariantLimit() {
		int limit = EditorPrefs.GetInt(SHADER_VARIANT_LIMIT_KEY);
		if (limit < SHADER_VARIANT_LIMIT)
			EditorPrefs.SetInt(SHADER_VARIANT_LIMIT_KEY, SHADER_VARIANT_LIMIT);

		// Also set it for Project Settings->ShaderGraph
		if (File.Exists(SHADER_GRAPH_SETTINGS_FILE)) {
			bool writeOut = false;
			string text = File.ReadAllText(SHADER_GRAPH_SETTINGS_FILE);
			Match match = Regex.Match(text, "shaderVariantLimit: (\\d+)");
			if (match.Success) {
				if (int.TryParse(match.Groups[1].Value, out int variantLimit)) {
					if (variantLimit < SHADER_VARIANT_LIMIT) {
						text = Regex.Replace(text, "shaderVariantLimit: " + variantLimit, "shaderVariantLimit: " + SHADER_VARIANT_LIMIT);
						writeOut = true;
					}
				}
			}
			match = Regex.Match(text, "overrideShaderVariantLimit: (\\d+)");
			if (match.Success) {
				if (int.TryParse(match.Groups[1].Value, out int overrideShaderLimit)) {
					if (overrideShaderLimit == 0) {
						text = Regex.Replace(text, "overrideShaderVariantLimit: 0", "overrideShaderVariantLimit: 1");
						writeOut = true;
					}
				}
			}
			if (writeOut)
				File.WriteAllText(SHADER_GRAPH_SETTINGS_FILE, text);
		}
	}
#endif
}
