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
            LineRenderer line_renderer = new_game_object.AddComponent<LineRenderer>();
            //line_renderer.castShadows = false;
            line_renderer.receiveShadows = false;
            return line_renderer;
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

        public static void DrawCross(Vector3 position, float size, Color colour)
        {
            Vector3 origin = position;
            DrawLine(origin + new Vector3(size, size, 0), origin + new Vector3(-size, -size, 0), colour);
            DrawLine(origin + new Vector3(-size, size, 0), origin + new Vector3(size, -size, 0), colour);
        }

        static void DrawLinePair(Vector3 surfaceNormal, Vector3 origin, Vector3 offset, float width, Color colour, float lineThickness = DefaultLineThickness)
        {
            Vector3 half_width_offset = surfaceNormal * width * 0.5f;

            DrawLine(origin - half_width_offset, origin + offset - half_width_offset, colour, lineThickness);

            if (width == 0)
            {
                return;
            }

            DrawLine(origin + half_width_offset, origin + offset + half_width_offset, colour, lineThickness);

            DrawLine(origin - half_width_offset, origin + half_width_offset, colour, lineThickness);
        }

        static Vector3 CalcSurfacePoint(float angle, Vector3 surfaceNormal, Vector3 origin, float radius)
        {
            //Vector3 new_point = surfaceNormal == Vector3.up ? new Vector3(0, 0, radius) : new Vector3(0, radius, 0);
            Vector3 new_point = Quaternion.FromToRotation(Vector3.up, surfaceNormal) * new Vector3(0, 0, radius);
            new_point = Quaternion.AngleAxis(angle, surfaceNormal) * new_point;
            return new_point + origin;
        }

        static Vector3 CalcSurfacePoint(int segmentIndex, Vector3 surfaceNormal, Vector3 origin, float radius, int segments)
        {
            float angle = 360.0f * (float)segmentIndex / segments;
            return CalcSurfacePoint(angle, surfaceNormal, origin, radius);

            /*
            Quaternion rotation = Quaternion.AngleAxis(360.0f * (float)segmentIndex / segments, surfaceNormal);
            Vector3 new_point = surfaceNormal == Vector3.forward ? new Vector3(0, radius, 0) : new Vector3(0, 0, radius);
            new_point = rotation * new_point + origin;
            return new_point;
            */
        }

        static public Vector3 HelperGetSurfaceNormal(Vector3 surfaceNormal)
        {
            //if (surfaceNormal == Vector3.zero && AppManagerSomerville.singleton && AppManagerSomerville.singleton.inGameCamera)
            //{
            //    return -AppManagerSomerville.singleton.inGameCamera.transform.forward;
            //}

            return surfaceNormal;
        }

        public static void DrawArc(Vector3 surfaceNormal, Vector3 origin, float radius, int segments, Color colour, float startAngle, float arcAngle, float thickness = DefaultLineThickness, float width = 0.0f)
        {
            surfaceNormal = HelperGetSurfaceNormal(surfaceNormal);

            Vector3 first_point = Vector3.zero;
            Vector3 prev_point = Vector3.zero;
            for (int idx = 0; idx < segments; idx++)
            {
                float anim = (float)idx / (float)(segments - 1);
                float angle = anim * arcAngle + startAngle;

                Vector3 new_point = CalcSurfacePoint(angle, surfaceNormal, origin, radius);

                if (idx == 0)
                {
                    first_point = new_point;
                }
                else
                {
                    DrawLinePair(surfaceNormal, prev_point, new_point - prev_point, width, colour, thickness);

                    if (idx == segments - 1)
                    {
                        DrawLinePair(surfaceNormal, new_point, origin - new_point, width, colour, thickness);
                        DrawLinePair(surfaceNormal, origin, first_point - origin, width, colour, thickness);
                    }
                }

                prev_point = new_point;
            }
        }


        public static void DrawArc(Vector3 surfaceNormal, Vector3 origin, float radius, Vector3 startSide, float arcAngle, int segments, Color colour, float thickness = DefaultLineThickness, float width = 0.0f)
        {
            Vector3 new_point = Quaternion.FromToRotation(Vector3.up, surfaceNormal) * new Vector3(0, 0, radius);
            float startAngle = Vector3.SignedAngle(new_point, startSide, surfaceNormal);

            DrawArc(surfaceNormal, origin, radius, segments, colour, startAngle, arcAngle, width, thickness);
        }

        public static void DrawCircle(Vector3 surfaceNormal, Vector3 origin, float radius, int segments, Color colour, float thickness = DefaultLineThickness, float width = 0.0f)
        {
            surfaceNormal = HelperGetSurfaceNormal(surfaceNormal);

            Vector3 first_point = Vector3.zero;
            Vector3 prev_point = Vector3.zero;
            for (int idx = 0; idx < segments; idx++)
            {
                Vector3 new_point = CalcSurfacePoint(idx, surfaceNormal, origin, radius, segments);

                if (idx == 0)
                {
                    first_point = new_point;
                }
                else
                {
                    DrawLinePair(surfaceNormal, prev_point, new_point - prev_point, width, colour, thickness);
                    if (idx == segments - 1)
                    {
                        DrawLinePair(surfaceNormal, new_point, first_point - new_point, width, colour, thickness);
                    }
                }

                prev_point = new_point;
            }
        }

        public static void DrawSphere(Vector3 origin, float radius, int segments, Color colour, float thickness = DefaultLineThickness, float width = 0.0f)
        {
            DrawCircle(Vector3.up, origin, radius, segments, colour, thickness, width);
            DrawCircle(Vector3.right, origin, radius, segments, colour, thickness, width);
            DrawCircle(Vector3.forward, origin, radius, segments, colour, thickness, width);
        }

        // Draws cylinder (before rotation, r1 = top radius, r2 = bottom radius)
        public static void DrawCylinder(Vector3 centre, Quaternion rotation, float length, float topRadius, float bottomRadius, int segments, Color colour, float thickness = DefaultLineThickness)
        {
            Vector3[] pts_1 = new Vector3[segments];
            Vector3[] pts_2 = new Vector3[segments];

            for (int idx = 0; idx < segments; idx++)
            {
                pts_1[idx] = CalcSurfacePoint(idx, Vector3.up, Vector3.zero, topRadius, segments) + new Vector3(0, length * 0.5f, 0);
                pts_2[idx] = CalcSurfacePoint(idx, Vector3.up, Vector3.zero, bottomRadius, segments) + new Vector3(0, length * -0.5f, 0);

                pts_1[idx] = rotation * pts_1[idx] + centre;
                pts_2[idx] = rotation * pts_2[idx] + centre;
            }

            for (int idx = 0; idx < segments; idx++)
            {
                DebugManager.DrawLine(pts_1[idx], pts_2[idx], colour);

                int next_idx = idx < segments - 1 ? idx + 1 : 0;
                DebugManager.DrawLine(pts_1[idx], pts_1[next_idx], colour, thickness);
                DebugManager.DrawLine(pts_2[idx], pts_2[next_idx], colour, thickness);
            }
        }

        public static void DrawStar(Vector3 surfaceNormal, Vector3 origin, float radius, float innerRadius, int segments, Color colour, float thickness = DefaultLineThickness, float width = 0.0f)
        {
            Vector3 first_point = Vector3.zero;
            Vector3 prev_point = Vector3.zero;
            for (int idx = 0; idx < segments; idx++)
            {
                float r = ((idx % 2) == 0) ? radius : innerRadius;

                Vector3 new_point = CalcSurfacePoint(idx, surfaceNormal, origin, r, segments);

                if (idx == 0)
                {
                    first_point = new_point;
                }
                else
                {
                    DrawLinePair(surfaceNormal, prev_point, new_point - prev_point, width, colour, thickness);
                    if (idx == segments - 1)
                    {
                        DrawLinePair(surfaceNormal, new_point, first_point - new_point, width, colour, thickness);
                    }
                }

                prev_point = new_point;
            }
        }
    }
}