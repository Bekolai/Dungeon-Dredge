//------------------------------------
//           OmniShade PBR
//     Copyright© 2025 OmniShade     
//------------------------------------

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;

[InitializeOnLoad]
public class RenderingPipelineDefines
{
	enum PipelineType {
		Unsupported,
		BuiltInPipeline,
		UniversalPipeline,
		HDPipeline
	}

	static RenderingPipelineDefines() {
		UpdateDefines();
	}

	static void UpdateDefines() {
		var pipeline = GetPipeline();
		if (pipeline == PipelineType.UniversalPipeline)
			AddDefine("UNITY_PIPELINE_URP");
		else
			RemoveDefine("UNITY_PIPELINE_URP");

		if (pipeline == PipelineType.HDPipeline)
			AddDefine("UNITY_PIPELINE_HDRP");
		else
			RemoveDefine("UNITY_PIPELINE_HDRP");
	}

	static PipelineType GetPipeline() {
		if (GraphicsSettings.defaultRenderPipeline != null) {
			var srpType = GraphicsSettings.defaultRenderPipeline.GetType().ToString();
			if (srpType.Contains("HDRenderPipelineAsset"))
				return PipelineType.HDPipeline;
			else if (srpType.Contains("UniversalRenderPipelineAsset") || srpType.Contains("LightweightRenderPipelineAsset"))
				return PipelineType.UniversalPipeline;
			else
				return PipelineType.Unsupported;
		}
		return PipelineType.BuiltInPipeline;
	}

	static void AddDefine(string define) {
		var definesList = GetDefines();
		if (!definesList.Contains(define)) {
			definesList.Add(define);
			SetDefines(definesList);
		}
	}

	public static void RemoveDefine(string define) {
		var definesList = GetDefines();
		if (definesList.Contains(define)) {
			definesList.Remove(define);
			SetDefines(definesList);
		}
	}

	public static List<string> GetDefines() {
		var target = EditorUserBuildSettings.activeBuildTarget;
		var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
		var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
		return defines.Split(';').ToList();
	}

	public static void SetDefines(List<string> definesList) {
		var target = EditorUserBuildSettings.activeBuildTarget;
		var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
		var defines = string.Join(";", definesList.ToArray());
		PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
	}
}
