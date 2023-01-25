using System.Collections.Generic;
using UnityEngine;

namespace SquigAPI
{
    public class DebugManager : MonoBehaviour
    {
        public struct DrawingData
        {
            public Color startColour;
            public Color endColour;
            public Vector3[] positions;
            public float thickness;
        }

        // List of potential materials that can be used to render a coloured line
        static string[] possibleLineShaderAssets = new string[]
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit" 
        };

        static DebugManager _instance;

        public bool enableDrawings = false;

        public int maxDrawings = 128;

        const float DefaultLineThickness = 0.1f;

        Material runtimeMaterial;

        public static DebugManager singleton
        {
            get
            {
                EnsureCreated();

                return _instance;
            }
        }

        static void EnsureCreated()
        {
            if(_instance == null)
            {
                // destroy old DebugManager for good measure
                DebugManager existing = FindObjectOfType<DebugManager>();
                if(existing)
                {
                    Destroy(existing);
                }

                Material material_for_line = null;
                for (int idx = 0; idx < possibleLineShaderAssets.Length; idx++)
                {
                    string path = possibleLineShaderAssets[idx];
                    Shader shader = Shader.Find(path);
                    if (shader)
                    {
                        material_for_line = new Material(shader);
                        if (material_for_line != null)
                        {
                            break;
                        }
                    }
                }

                string name = string.Format("*** DebugManager{0} ***", material_for_line == null ? "[Error:1]" : "");
                GameObject game_object = new GameObject(name);
                _instance = game_object.AddComponent<DebugManager>();
                _instance.runtimeMaterial = material_for_line;
            }
        }

        List<DrawingData> drawings = new List<DrawingData>();
        List<LineRenderer> lineRenderers = new List<LineRenderer>();

        LineRenderer AddLineRenderer()
        {
            GameObject new_game_object = new GameObject("lr");
            new_game_object.transform.SetParent(gameObject.transform);
            return new_game_object.AddComponent<LineRenderer>();
        }

        void CreateVisuals()
        {
            int total = 0;

            total = Mathf.Max(drawings.Count, lineRenderers.Count);

            for(int idx = 0; idx < total; idx ++)
            {
                if(lineRenderers.Count <= idx)
                {
                    lineRenderers.Add(AddLineRenderer());
                }

                LineRenderer line_renderer = lineRenderers[idx];

                if (line_renderer)
                {
                    line_renderer.enabled = (drawings.Count > idx);

                    if (line_renderer.enabled)
                    {
                        DrawingData drawing = drawings[idx];

                        line_renderer.startColor = drawing.startColour;
                        line_renderer.endColor = drawing.endColour;
                        line_renderer.SetPositions(drawing.positions);
                        line_renderer.startWidth = drawing.thickness;
                        line_renderer.endWidth = drawing.thickness;
                        line_renderer.material = runtimeMaterial;
                    }
                }
            }

            drawings.Clear();
        }

        int speedModifierIndex = 4;
        float[] speedModifierTable = new float[] { 0.0f, 0.125f, 0.25f, 0.5f, 1.0f, 2.0f, 4.0f, 8.0f };

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                enableDrawings = !enableDrawings;
                Debug.Log(string.Format("Debug toggled via keypress - {0}", enableDrawings ? "on" : "off"));
            }


            bool speed_up = Input.GetKeyDown(KeyCode.UpArrow);
            bool speed_down = Input.GetKeyDown(KeyCode.DownArrow);
            bool speed_reset = Input.GetKeyDown(KeyCode.Keypad0);

            if (speed_up || speed_down || speed_reset)
            {
                if(speed_reset)
                {
                    speedModifierIndex = 3;
                }
                else if(speed_up)
                {
                    if (speedModifierIndex < speedModifierTable.Length-1)
                    {
                        speedModifierIndex++;
                    }
                }
                else
                {
                    if (speedModifierIndex > 0)
                    {
                        speedModifierIndex--;
                    }
                }


                float scale = speedModifierTable[speedModifierIndex];
                Time.timeScale = scale;
                Debug.Log(string.Format("TimeScale = x{0}", scale));
            }
        }

        private void LateUpdate()
        {
            CreateVisuals();
        }

        void _AddLine(Vector3 start, Vector3 end, Color colourStart, Color colourEnd, float thickness)
        {
            if(!enableDrawings)
            {
                return;
            }

            DrawingData new_drawing = new DrawingData();
            new_drawing.startColour = colourStart;
            new_drawing.endColour = colourEnd;
            new_drawing.positions = new Vector3[] { start, end };
            new_drawing.thickness = thickness;

            drawings.Add(new_drawing);
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color colour, float lineThickness = DefaultLineThickness)
        {
            singleton._AddLine(start, end, colour, colour, lineThickness);
        }

        public static void AddLine(Vector3 start, Vector3 end, Color colourStart, Color colourEnd, float lineThickness = DefaultLineThickness)
        {
            singleton._AddLine(start, end, colourStart, colourEnd, lineThickness);
        }
    }
}