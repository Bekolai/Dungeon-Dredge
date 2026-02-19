//------------------------------------
//           OmniShade PBR
//     Copyright© 2025 OmniShade     
//------------------------------------

using System.Collections.Generic;
using UnityEngine;

/**
 * This component supports animating the texture UVs of the shader.
 **/
[RequireComponent(typeof(Renderer))]
public class OmniShadePBRAnimateTexture : MonoBehaviour {

	public enum OmniShadeTexture {
		// DO NOT CHANGE THIS ORDER
		BaseMap, MetallicGlossMap, BumpMap, OcclusionMap, EmissionMap,
		DetailMap, Layer1, Layer2, Layer3, BumpMap2, HeightColorsTex,
	}

	[System.Serializable]
	public class AnimatedTexture {
		public int matIndex;
		public OmniShadeTexture texture;
		public Vector2 speed;

		[Header("Ping Pong")]
		public bool pingPong = false;
		[Range(0, 100)] public float frequency;

		[Header("Frame Animation")]
		[Range(1, 60)] public int FPS;
		public Texture2D[] frames;

		// Non user-adjustable
		[HideInInspector] public int textureID;
		[HideInInspector] public Vector2 currentUV;
		[HideInInspector] public int currentFrame;
		[HideInInspector] public float currentFrameTime;
	}

	public List<AnimatedTexture> texturesToAnimate;

	Material[] mats;

	void Start() {
		if (this.texturesToAnimate == null)
			return;

		this.mats = this.GetComponent<Renderer>().materials;
		foreach (var animTex in this.texturesToAnimate) {
			if (animTex.matIndex < 0 || animTex.matIndex >= this.mats.Length) {
				animTex.textureID = -1;
				Debug.LogError("Invalid material index " + animTex.matIndex);
				continue;
			}
			var mat = this.mats[animTex.matIndex];
			if (mat == null) {
				Debug.LogError("Null material at index " + animTex.matIndex);
				continue;
			}

			// Initialize texture ID and UV
			string texName = "_" + animTex.texture.ToString();
			animTex.textureID = Shader.PropertyToID(texName);
			animTex.currentUV = mat.GetTextureOffset(animTex.textureID);
		}
	}

	void Update() {
		if (this.texturesToAnimate == null)
			return;

		// Loop textures and animate UVs
		for (int i = 0; i < this.texturesToAnimate.Count; i++) {
			var animTex = this.texturesToAnimate[i];
			if (animTex.textureID == -1)
				continue;
			var mat = this.mats[animTex.matIndex];
			if (mat == null)
				continue;

			// Animate movement
			if (animTex.speed.x != 0 || animTex.speed.y != 0) {
				var uv = animTex.currentUV;
				Vector2 speed = animTex.speed;
				if (animTex.pingPong)
					speed *= Mathf.Sin(Time.time * animTex.frequency);
				uv += speed * Time.deltaTime;
				uv.x %= 1.0f;
				uv.y %= 1.0f;

				animTex.currentUV = uv;
				mat.SetTextureOffset(animTex.textureID, uv);
			}

			// Frame animation
			if (animTex.frames.Length > 0 && animTex.FPS > 0) {
				float timePerFrame = 1.0f / animTex.FPS;
				animTex.currentFrameTime += Time.deltaTime;
				int frameInc = (int)(animTex.currentFrameTime / timePerFrame);
				if (frameInc > 0) {
					animTex.currentFrame = (animTex.currentFrame + frameInc) % animTex.frames.Length;
					animTex.currentFrameTime %= timePerFrame;
					mat.SetTexture(animTex.textureID, animTex.frames[animTex.currentFrame]);
				}
			}
		}
	}
}
