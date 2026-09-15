using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public static class MyCobot320Fk
    {
        // Modified (Craig) DH: alpha_{i-1}, a_{i-1} (mm), d_i (mm), theta offset (rad).
        // Validated against the real arm's angles_to_coords (matches to <0.1mm).
        static readonly float[][] DH =
        {
            new[] { 0f,             0f,     173.9f,  0f },
            new[] { Mathf.PI / 2f,  0f,     0f,     -Mathf.PI / 2f },
            new[] { 0f,            -135f,   0f,      0f },
            new[] { 0f,            -120f,   88.78f, -Mathf.PI / 2f },
            new[] { Mathf.PI / 2f,  0f,     95.0f,   0f },
            new[] { -Mathf.PI / 2f, 0f,     65.5f,   0f }
        };

        // Panel joint limits (2-3 deg inside mechanical so joints don't stick).
        public static readonly Vector2[] Limits =
        {
            new Vector2(-163f, 163f), new Vector2(-134f, 134f), new Vector2(-163f, 163f),
            new Vector2(-163f, 163f), new Vector2(-163f, 163f), new Vector2(-173f, 173f)
        };

        static Matrix4x4 Link(float alpha, float a, float d, float theta)
        {
            float ct = Mathf.Cos(theta), st = Mathf.Sin(theta);
            float ca = Mathf.Cos(alpha), sa = Mathf.Sin(alpha);
            Matrix4x4 m = Matrix4x4.identity;
            m.m00 = ct;    m.m01 = -st;    m.m02 = 0f;  m.m03 = a;
            m.m10 = st*ca; m.m11 = ct*ca;  m.m12 = -sa; m.m13 = -sa*d;
            m.m20 = st*sa; m.m21 = ct*sa;  m.m22 = ca;  m.m23 = ca*d;
            return m;
        }

        // anglesDeg = real robot degrees J1..J6. toolMm = TCP offset along tool z.
        public static Matrix4x4 TcpBaseMatrix(float[] anglesDeg, float toolMm)
        {
            Matrix4x4 t = Matrix4x4.identity;
            for (int i = 0; i < 6; i++)
                t *= Link(DH[i][0], DH[i][1], DH[i][2], anglesDeg[i] * Mathf.Deg2Rad + DH[i][3]);
            if (Mathf.Abs(toolMm) > 0.0001f) t *= Link(0f, 0f, toolMm, 0f);
            return t;
        }

        public static Vector3 PosMm(Matrix4x4 t) => new Vector3(t.m03, t.m13, t.m23);
        public static Vector3 ToolZ(Matrix4x4 t) => new Vector3(t.m02, t.m12, t.m22);
    }
}