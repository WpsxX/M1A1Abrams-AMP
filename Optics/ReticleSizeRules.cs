using System;
using Reticle;

namespace M1A1Abrams
{
    internal static class ReticleSizeRules
    {
        internal static float Scale(float fov, float reference)
        {
            double current = Math.Max(0.01, Math.Min(170, fov));
            double baseline = Math.Max(0.01, Math.Min(170, reference));
            return (float)(Math.Tan(current * Math.PI / 360) / Math.Tan(baseline * Math.PI / 360));
        }
    }

    // Store scalars, not Position references: Position is a mutable class in GHPC.
    // Repeated zoom changes must always start from the original geometry.
    internal sealed class ReticleElementSize
    {
        private readonly ReticleTree.TransformElement element;
        private readonly float x, y, length, thickness, fontSize;

        internal ReticleElementSize(ReticleTree.TransformElement element)
        {
            this.element = element;
            x = element.position.x;
            y = element.position.y;
            var line = element as ReticleTree.Line;
            var circle = element as ReticleTree.Circle;
            var text = element as ReticleTree.Text;
            if (line != null) { length = line.length.MRADS; thickness = line.thickness.MRADS; }
            if (circle != null) { length = circle.radius.MRADS; thickness = circle.thickness.MRADS; }
            if (text != null) fontSize = text.fontSize;
        }

        internal void Apply(float scale)
        {
            element.position.x = x * scale;
            element.position.y = y * scale;
            var line = element as ReticleTree.Line;
            var circle = element as ReticleTree.Circle;
            var text = element as ReticleTree.Text;
            if (line != null) { line.length.MRADS = length * scale; line.thickness.MRADS = thickness * scale; }
            if (circle != null) { circle.radius.MRADS = length * scale; circle.thickness.MRADS = thickness * scale; }
            if (text != null) text.fontSize = fontSize * scale;
        }
    }
}
