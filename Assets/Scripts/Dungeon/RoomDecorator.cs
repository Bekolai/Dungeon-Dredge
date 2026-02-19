using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.Dungeon
{
    /// <summary>
    /// Handles spawning random decorations in a room based on a RoomTheme.
    /// Attach this to room prefabs alongside the Room component.
    /// </summary>
    public class RoomDecorator : MonoBehaviour
    {
        [Header("Decoration Containers")]
        [SerializeField] private Transform decorationsParent;
        [SerializeField] private Transform lightingParent;

        [Header("Spawn Point Containers")]
        [Tooltip("Empty transforms marking where floor props can spawn")]
        [SerializeField] private Transform floorSpawnPoints;
        [Tooltip("Empty transforms marking wall positions")]
        [SerializeField] private Transform wallSpawnPoints;
        [Tooltip("Empty transforms marking corners")]
        [SerializeField] private Transform cornerSpawnPoints;
        [Tooltip("Points specifically for light sources")]
        [SerializeField] private Transform lightSpawnPoints;

        [Header("Fallback Settings")]
        [SerializeField] private Vector2 roomSize = new Vector2(16f, 16f);
        [SerializeField] private float wallOffset = 0.5f;
        [SerializeField] private float cornerOffset = 0.75f;
        [SerializeField] private float doorwayHalfWidth = 2.5f;
        [SerializeField] private float doorwayPadding = 1f;

        // Spawned decorations for cleanup
        private List<GameObject> spawnedDecorations = new List<GameObject>();
        private Room room;

        /// <summary>
        /// Best-effort runtime setup for missing serialized references.
        /// Keeps prefab authoring optional for procedural/test rooms.
        /// </summary>
        public void EnsureRuntimeReferences()
        {
            if (room == null)
                room = GetComponent<Room>();

            if (decorationsParent == null)
                decorationsParent = FindChildRecursive(transform, "Decorations");

            if (lightingParent == null)
                lightingParent = FindChildRecursive(transform, "Lighting");

            // Fallback to room root if named containers are missing.
            if (decorationsParent == null)
                decorationsParent = transform;
            if (lightingParent == null)
                lightingParent = decorationsParent;

            if (floorSpawnPoints == null)
                floorSpawnPoints = FindChildRecursive(transform, "FloorSpawnPoints");
            if (wallSpawnPoints == null)
                wallSpawnPoints = FindChildRecursive(transform, "WallSpawnPoints");
            if (cornerSpawnPoints == null)
                cornerSpawnPoints = FindChildRecursive(transform, "CornerSpawnPoints");
            if (lightSpawnPoints == null)
                lightSpawnPoints = FindChildRecursive(transform, "LightSpawnPoints");
        }

        /// <summary>
        /// Decorate this room using the given theme
        /// </summary>
        public void Decorate(RoomTheme theme, RoomType roomType)
        {
            if (theme == null) return;

            ClearDecorations();

            // Spawn light sources
            SpawnLightSources(theme);

            // Spawn decorations based on room type
            switch (roomType)
            {
                case RoomType.Portal:
                    // Minimal decorations for portal room (spawn/extract)
                    SpawnFloorClutter(theme, 0.5f);
                    break;

                case RoomType.Empty:
                    SpawnLargeProps(theme);
                    SpawnSmallProps(theme);
                    SpawnFloorClutter(theme, 1f);
                    SpawnWallDecorations(theme);
                    break;

                case RoomType.Loot:
                    // More props in loot rooms
                    SpawnLargeProps(theme, 1.5f);
                    SpawnSmallProps(theme, 1.5f);
                    SpawnFloorClutter(theme, 0.8f);
                    SpawnWallDecorations(theme);
                    SpawnCornerProps(theme);
                    TrySpawnSpecialProp(theme);
                    break;

                case RoomType.Enemy:
                    // Combat-oriented, less clutter
                    SpawnLargeProps(theme, 0.7f);
                    SpawnSmallProps(theme, 0.5f);
                    SpawnFloorClutter(theme, 0.6f);
                    SpawnWallDecorations(theme, 0.8f);
                    break;

                // Extraction room was merged into Portal; keep special atmosphere for Boss instead
                case RoomType.Boss:
                    // Special / grand atmosphere
                    SpawnFloorClutter(theme, 0.5f);
                    SpawnWallDecorations(theme, 1.2f);
                    SpawnCornerProps(theme);
                    TrySpawnSpecialProp(theme, 0.5f); // Higher chance for special prop
                    break;
            }
        }

        private void SpawnLightSources(RoomTheme theme)
        {
            bool hasPrefabs = theme.lightSources != null && theme.lightSources.Length > 0;
            int count = Random.Range(theme.minLightSources, theme.maxLightSources + 1);

            if (hasPrefabs)
            {
                // Use assigned light source prefabs
                var prefabs = theme.GetMultipleFromPool(theme.lightSources, count);

                if (lightSpawnPoints != null && lightSpawnPoints.childCount > 0)
                {
                    var points = GetShuffledChildPositions(lightSpawnPoints);
                    int spawned = 0;
                    foreach (var point in points)
                    {
                        if (spawned >= prefabs.Length) break;
                        if (!IsValidWallSpawnPosition(point)) continue;

                        SpawnDecoration(prefabs[spawned], point, lightingParent ?? decorationsParent, true);
                        spawned++;
                    }
                }
                else
                {
                    for (int i = 0; i < prefabs.Length; i++)
                    {
                        Vector3 pos = GetRandomWallPosition();
                        pos.y = 2.5f;
                        SpawnDecoration(prefabs[i], pos, lightingParent ?? decorationsParent, false);
                    }
                }
            }
            else
            {
                // RUNTIME FALLBACK: Create procedural torch lights
                // This ensures rooms are always lit with atmospheric torches even without prefabs
                SpawnRuntimeTorches(theme, count);
            }
        }

        /// <summary>
        /// Creates procedural wall-mounted torch lights when no light source prefabs are assigned.
        /// Each torch consists of a point light with flickering + a simple visual indicator.
        /// </summary>
        private void SpawnRuntimeTorches(RoomTheme theme, int count)
        {
            Transform parent = lightingParent ?? decorationsParent ?? transform;
            List<Vector3> usedPositions = new List<Vector3>();
            float minDistBetweenTorches = roomSize.x / (count + 1); // Spread torches evenly-ish

            for (int i = 0; i < count; i++)
            {
                // Find a valid wall position that's spaced from other torches
                Vector3 pos = Vector3.zero;
                bool found = false;

                for (int attempt = 0; attempt < 30; attempt++)
                {
                    Vector3 candidate = GetRandomWallPosition();
                    candidate.y = 2.5f; // Wall torch height

                    // Check minimum distance from other torches
                    bool tooClose = false;
                    foreach (var used in usedPositions)
                    {
                        if (Vector3.Distance(candidate, used) < minDistBetweenTorches)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose && IsValidWallSpawnPosition(candidate))
                    {
                        pos = candidate;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Fallback: place evenly along room perimeter
                    float t = (float)i / count;
                    pos = GetPerimeterPosition(t);
                    pos.y = 2.5f;
                }

                usedPositions.Add(pos);

                // Create the torch GameObject
                GameObject torchObj = new GameObject($"RuntimeTorch_{i}");
                torchObj.transform.SetParent(parent);
                torchObj.transform.position = pos;

                // Face inward toward room center
                Vector3 toCenter = (transform.position - pos);
                toCenter.y = 0;
                if (toCenter.sqrMagnitude > 0.01f)
                    torchObj.transform.rotation = Quaternion.LookRotation(toCenter.normalized);

                // === Point Light - Small focused pool, not a floodlight ===
                // Clamp values: old theme assets may have 0 for new fields
                float intensity = theme.torchIntensity > 0.01f ? theme.torchIntensity : 0.8f;
                float range = theme.torchRange > 1f ? theme.torchRange : 5f;
                Color tColor = (theme.torchColor.r + theme.torchColor.g + theme.torchColor.b) > 0.1f
                    ? theme.torchColor : new Color(1f, 0.7f, 0.4f);

                Light torchLight = torchObj.AddComponent<Light>();
                torchLight.type = LightType.Point;
                torchLight.color = tColor;
                torchLight.intensity = intensity;
                torchLight.range = range;
                torchLight.shadows = LightShadows.Soft;
                torchLight.shadowStrength = 0.85f;
                torchLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
                torchLight.renderMode = LightRenderMode.ForcePixel;

                // === Flicker Effect ===
                TorchFlicker flicker = torchObj.AddComponent<TorchFlicker>();
                flicker.Configure(tColor, intensity, range);

                // === Simple Visual: Small emissive sphere as torch "flame" ===
                GameObject flameVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flameVisual.name = "FlameVisual";
                flameVisual.transform.SetParent(torchObj.transform);
                flameVisual.transform.localPosition = Vector3.zero;
                flameVisual.transform.localScale = new Vector3(0.15f, 0.22f, 0.15f);

                // Remove collider so it doesn't interfere with gameplay
                var col = flameVisual.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                // Create emissive material for the flame
                var renderer = flameVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material flameMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (flameMat.shader.name == "Hidden/InternalErrorShader")
                    {
                        // Fallback for non-URP setups
                        flameMat = new Material(Shader.Find("Standard"));
                    }
                    flameMat.color = tColor;
                    flameMat.EnableKeyword("_EMISSION");
                    flameMat.SetColor("_EmissionColor", tColor * 2f);
                    renderer.material = flameMat;
                }

                // === Small holder visual (bracket/sconce) ===
                GameObject holder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                holder.name = "TorchHolder";
                holder.transform.SetParent(torchObj.transform);
                holder.transform.localPosition = new Vector3(0f, -0.15f, 0f);
                holder.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);

                var holderCol = holder.GetComponent<Collider>();
                if (holderCol != null) Object.Destroy(holderCol);

                var holderRenderer = holder.GetComponent<Renderer>();
                if (holderRenderer != null)
                {
                    Material holderMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (holderMat.shader.name == "Hidden/InternalErrorShader")
                        holderMat = new Material(Shader.Find("Standard"));
                    holderMat.color = new Color(0.15f, 0.1f, 0.08f); // Dark iron/wood color
                    holderRenderer.material = holderMat;
                }

                spawnedDecorations.Add(torchObj);
            }
        }

        /// <summary>
        /// Get a position along the room perimeter at normalized parameter t [0,1].
        /// Used to evenly distribute torches as a fallback.
        /// </summary>
        private Vector3 GetPerimeterPosition(float t)
        {
            float halfX = roomSize.x / 2f - wallOffset;
            float halfZ = roomSize.y / 2f - wallOffset;
            float perimeter = 2f * (halfX + halfZ) * 2f;
            float dist = t * perimeter;

            // Walk around the perimeter: N -> E -> S -> W
            float northLen = halfX * 2f;
            float eastLen = halfZ * 2f;
            float southLen = halfX * 2f;

            if (dist < northLen)
            {
                float along = -halfX + dist;
                return transform.position + new Vector3(along, 0, halfZ);
            }
            dist -= northLen;

            if (dist < eastLen)
            {
                float along = halfZ - dist;
                return transform.position + new Vector3(halfX, 0, along);
            }
            dist -= eastLen;

            if (dist < southLen)
            {
                float along = halfX - dist;
                return transform.position + new Vector3(along, 0, -halfZ);
            }
            dist -= southLen;

            {
                float along = -halfZ + dist;
                return transform.position + new Vector3(-halfX, 0, along);
            }
        }

        private void SpawnLargeProps(RoomTheme theme, float multiplier = 1f)
        {
            if (theme.largeProps == null || theme.largeProps.Length == 0) return;

            int count = Mathf.RoundToInt(Random.Range(theme.minLargeProps, theme.maxLargeProps + 1) * multiplier);
            var prefabs = theme.GetMultipleFromPool(theme.largeProps, count);

            foreach (var prefab in prefabs)
            {
                Vector3 pos = GetRandomFloorPosition(2f); // Keep away from center
                SpawnDecoration(prefab, pos, decorationsParent, true);
            }
        }

        private void SpawnSmallProps(RoomTheme theme, float multiplier = 1f)
        {
            if (theme.smallProps == null || theme.smallProps.Length == 0) return;

            int count = Mathf.RoundToInt(Random.Range(theme.minSmallProps, theme.maxSmallProps + 1) * multiplier);
            var prefabs = theme.GetMultipleFromPool(theme.smallProps, count);

            foreach (var prefab in prefabs)
            {
                Vector3 pos = GetRandomFloorPosition(1f);
                SpawnDecoration(prefab, pos, decorationsParent, true);
            }
        }

        private void SpawnFloorClutter(RoomTheme theme, float multiplier = 1f)
        {
            if (theme.floorClutter == null || theme.floorClutter.Length == 0) return;

            int count = Mathf.RoundToInt(Random.Range(theme.minFloorClutter, theme.maxFloorClutter + 1) * multiplier);
            var prefabs = theme.GetMultipleFromPool(theme.floorClutter, count);

            foreach (var prefab in prefabs)
            {
                Vector3 pos = GetRandomFloorPosition(0.5f);
                SpawnDecoration(prefab, pos, decorationsParent, true);
            }
        }

        private void SpawnWallDecorations(RoomTheme theme, float multiplier = 1f)
        {
            if (theme.wallDecorations == null || theme.wallDecorations.Length == 0) return;

            int count = Mathf.RoundToInt(Random.Range(theme.minWallDecorations, theme.maxWallDecorations + 1) * multiplier);
            var prefabs = theme.GetMultipleFromPool(theme.wallDecorations, count);

            if (wallSpawnPoints != null && wallSpawnPoints.childCount > 0)
            {
                var points = GetShuffledChildTransforms(wallSpawnPoints);
                int spawned = 0;
                foreach (var point in points)
                {
                    if (spawned >= prefabs.Length) break;
                    if (!IsValidWallSpawnPosition(point.position)) continue;

                    SpawnDecoration(prefabs[spawned], point.position, decorationsParent, false, point.rotation);
                    spawned++;
                }
            }
            else
            {
                foreach (var prefab in prefabs)
                {
                    Vector3 pos = GetRandomWallPosition();
                    pos.y = Random.Range(1f, 2.5f);
                    Quaternion rot = GetWallRotation(pos);
                    SpawnDecoration(prefab, pos, decorationsParent, false, rot);
                }
            }
        }

        private void SpawnCornerProps(RoomTheme theme)
        {
            if (theme.cornerProps == null || theme.cornerProps.Length == 0) return;

            int count = Random.Range(theme.minCornerProps, theme.maxCornerProps + 1);
            var prefabs = theme.GetMultipleFromPool(theme.cornerProps, count);

            if (cornerSpawnPoints != null && cornerSpawnPoints.childCount > 0)
            {
                var points = GetShuffledChildPositions(cornerSpawnPoints);
                for (int i = 0; i < Mathf.Min(prefabs.Length, points.Count); i++)
                {
                    SpawnDecoration(prefabs[i], points[i], decorationsParent, true);
                }
            }
            else
            {
                // Fallback: use actual corners
                Vector3[] corners = GetCornerPositions();
                for (int i = 0; i < Mathf.Min(prefabs.Length, corners.Length); i++)
                {
                    SpawnDecoration(prefabs[i], corners[i], decorationsParent, true);
                }
            }
        }

        private void TrySpawnSpecialProp(RoomTheme theme, float chanceMultiplier = 1f)
        {
            if (theme.specialProps == null || theme.specialProps.Length == 0) return;

            if (Random.value <= theme.specialPropChance * chanceMultiplier)
            {
                var prefab = theme.GetRandomFromPool(theme.specialProps);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomFloorPosition(3f);
                    SpawnDecoration(prefab, pos, decorationsParent, true);
                }
            }
        }

        private GameObject SpawnDecoration(GameObject prefab, Vector3 position, Transform parent, 
            bool randomYRotation, Quaternion? overrideRotation = null)
        {
            if (prefab == null) return null;

            Quaternion rotation = overrideRotation ?? Quaternion.identity;
            if (randomYRotation && !overrideRotation.HasValue)
            {
                rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }

            GameObject instance = Instantiate(prefab, position, rotation, parent ?? transform);
            spawnedDecorations.Add(instance);
            return instance;
        }

        public void ClearDecorations()
        {
            foreach (var decoration in spawnedDecorations)
            {
                if (decoration != null)
                {
                    if (Application.isPlaying)
                        Destroy(decoration);
                    else
                        DestroyImmediate(decoration);
                }
            }
            spawnedDecorations.Clear();
        }

        #region Position Helpers

        private Vector3 GetRandomFloorPosition(float edgeMargin = 1f)
        {
            float halfX = roomSize.x / 2f - edgeMargin;
            float halfZ = roomSize.y / 2f - edgeMargin;
            
            return transform.position + new Vector3(
                Random.Range(-halfX, halfX),
                0f,
                Random.Range(-halfZ, halfZ)
            );
        }

        private Vector3 GetRandomWallPosition()
        {
            float halfX = roomSize.x / 2f - wallOffset;
            float halfZ = roomSize.y / 2f - wallOffset;

            const int maxAttempts = 24;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 0: North, 1: South, 2: East, 3: West
                int wall = Random.Range(0, 4);
                float along = (wall == 0 || wall == 1)
                    ? Random.Range(-halfX, halfX)
                    : Random.Range(-halfZ, halfZ);

                if (IsInDoorwayBand(wall, along))
                    continue;

                return wall switch
                {
                    0 => transform.position + new Vector3(along, 0, halfZ),
                    1 => transform.position + new Vector3(along, 0, -halfZ),
                    2 => transform.position + new Vector3(halfX, 0, along),
                    _ => transform.position + new Vector3(-halfX, 0, along)
                };
            }

            // Fallback to a safe corner-adjacent position if random attempts all fail.
            return transform.position + new Vector3(halfX - 1f, 0f, halfZ);
        }

        private Quaternion GetWallRotation(Vector3 position)
        {
            Vector3 toCenter = (transform.position - position).normalized;
            return Quaternion.LookRotation(toCenter);
        }

        private Vector3[] GetCornerPositions()
        {
            float halfX = roomSize.x / 2f - cornerOffset;
            float halfZ = roomSize.y / 2f - cornerOffset;
            Vector3 center = transform.position;
            
            return new Vector3[]
            {
                center + new Vector3(-halfX, 0, halfZ),  // NW
                center + new Vector3(halfX, 0, halfZ),   // NE
                center + new Vector3(-halfX, 0, -halfZ), // SW
                center + new Vector3(halfX, 0, -halfZ)   // SE
            };
        }

        private List<Vector3> GetShuffledChildPositions(Transform parent)
        {
            var positions = new List<Vector3>();
            foreach (Transform child in parent)
            {
                positions.Add(child.position);
            }
            ShuffleList(positions);
            return positions;
        }

        private List<Transform> GetShuffledChildTransforms(Transform parent)
        {
            var transforms = new List<Transform>();
            foreach (Transform child in parent)
            {
                transforms.Add(child);
            }
            ShuffleList(transforms);
            return transforms;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null) return null;
            if (root.name == childName) return root;

            foreach (Transform child in root)
            {
                Transform match = FindChildRecursive(child, childName);
                if (match != null)
                    return match;
            }

            return null;
        }

        private bool IsValidWallSpawnPosition(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - transform.position;
            float halfX = roomSize.x / 2f - wallOffset;
            float halfZ = roomSize.y / 2f - wallOffset;
            float tolerance = 0.75f;

            if (Mathf.Abs(local.z - halfZ) <= tolerance) // North
                return !IsInDoorwayBand(0, local.x);
            if (Mathf.Abs(local.z + halfZ) <= tolerance) // South
                return !IsInDoorwayBand(1, local.x);
            if (Mathf.Abs(local.x - halfX) <= tolerance) // East
                return !IsInDoorwayBand(2, local.z);
            if (Mathf.Abs(local.x + halfX) <= tolerance) // West
                return !IsInDoorwayBand(3, local.z);

            return true;
        }

        private bool IsInDoorwayBand(int wall, float alongAxis)
        {
            if (!IsWallPassable(wall))
                return false;

            float doorwayLimit = doorwayHalfWidth + doorwayPadding;
            return Mathf.Abs(alongAxis) <= doorwayLimit;
        }

        private bool IsWallPassable(int wall)
        {
            if (room == null)
                room = GetComponent<Room>();
            if (room == null)
                return true;

            DoorDirection direction = wall switch
            {
                0 => DoorDirection.North,
                1 => DoorDirection.South,
                2 => DoorDirection.East,
                _ => DoorDirection.West
            };

            Door door = room.GetDoor(direction);
            if (door == null)
                return false;

            return door.Mode != DoorMode.Blocked;
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw room bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up, 
                new Vector3(roomSize.x, 4f, roomSize.y));

            // Draw corners
            Gizmos.color = Color.yellow;
            foreach (var corner in GetCornerPositions())
            {
                Gizmos.DrawWireSphere(corner, 0.3f);
            }
        }
#endif
    }
}
