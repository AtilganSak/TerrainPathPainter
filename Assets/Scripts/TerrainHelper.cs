using UnityEngine;
namespace TerrainPathPainter
{
    public static class TerrainHelper
    {
        public static bool IsWorldPointOnTopOfTerrain(Vector3 worldPos, Terrain terrain)
        {
            // doesn't take y into consideration.
            if(terrain.transform.position.x <= worldPos.x && worldPos.x <= (terrain.transform.position.x + terrain.terrainData.size.x))
            {
            }
            else
                return false;

            if(terrain.transform.position.z <= worldPos.z && worldPos.z <= (terrain.transform.position.z + terrain.terrainData.size.z))
            {
            }
            else
                return false;

            return true;
        }

        public static float TerrainHeightToWorldHeight(float terrainHeight, Terrain terrain)
        {
            if(terrainHeight < 0 || terrainHeight > 1)
            {
                Debug.LogWarning("TerrainHeightToWorldHeight received out of range terrainHeight : " + terrainHeight);
            }

            return (terrainHeight * terrain.terrainData.size.y) + terrain.gameObject.transform.position.y;
        }

        /// <summary>
        /// -3 pozisyonundaki, Terrain Height'i 40 olan bir terrainde 
        /// weightHeight=37 terrainin en üst noktasıdır, 1 döner.
        /// </summary>
        /// <param name="wHeight">transform.position.y</param>
        /// <param name="terrain">Terrain</param>
        /// <returns>Float 0-1</returns>
        public static float WorldHeightToTerrainHeight(float worldHeight, Terrain terrain)
        {
            float terrainHeight = (worldHeight - terrain.transform.position.y) / terrain.terrainData.size.y; ;

            if(terrainHeight < 0 || terrainHeight > 1)
            {
                // Debug.LogWarning("WorldHeightToTerrainHeight calculated out of range terrainHeight : " + terrainHeight);
                terrainHeight = Mathf.Clamp01(terrainHeight);
            }

            return terrainHeight;
        }

        public static Vector2Int WorldPositionToTerrainMap(Vector3 worldPosition, Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            var netPosition = worldPosition - terrain.gameObject.transform.position;
            int xzRes = terrainData.alphamapResolution; // ex:513
            float xScale = xzRes / terrainData.size.x; // width  500 1 world unit heightmap'de hangi noktaya geliyor... 
            float zScale = xzRes / terrainData.size.z; // length 400

            int x = Mathf.Clamp(Mathf.RoundToInt(netPosition.x * xScale), 0, xzRes);
            int y = Mathf.Clamp(Mathf.RoundToInt(netPosition.z * zScale), 0, xzRes);
            return new Vector2Int(x, y);
        }

        public static Vector3 TerrainPointToWorldPosition(int x, int y, Terrain terrain)
        {
            TerrainData terrainData = terrain.terrainData;
            int xzRes = terrainData.heightmapResolution; // ex:513
            float xScale = xzRes / terrainData.size.x; // width  500 1 world unit heightmap'de hangi noktaya geliyor... 
            float zScale = xzRes / terrainData.size.z; // length 400

            float xCoord = (x + terrain.gameObject.transform.position.x * xScale) / xScale;
            float zCoord = (y + terrain.gameObject.transform.position.z * zScale) / zScale;
            float height = TerrainHeightToWorldHeight(terrainData.GetHeight(x, y) / terrainData.size.y, terrain);

            return new Vector3(xCoord, height, zCoord);
        }
    }
}
