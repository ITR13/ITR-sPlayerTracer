using UnityEngine;

namespace VrcTracer
{
    [System.Serializable]
    public class TracerConfig
    {
        public KeyCode hold = KeyCode.LeftControl, trigger = KeyCode.T;
        public SerializedColor blockedColor = new SerializedColor(1, 0, 0, 1);
        public SerializedColor errorColor = new SerializedColor(1, 0, 0, 1);

    }

    [System.Serializable]
    public class SerializedColor
    {
        public float Red, Green, Blue, Alpha;

        public SerializedColor() { }

        public SerializedColor(float red, float green, float blue, float alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public static implicit operator Color(SerializedColor Color)
        {
            return new Color(Color.Red, Color.Green, Color.Blue, Color.Alpha);
        }
    }
}
