using System;
using UnityEngine;

namespace VrcTracer
{
    [Serializable]
    public class TracerConfig
    {
        public SerializedColor blockedColor = new SerializedColor(1, 0, 0, 1);
        public SerializedVector3 destinationOffset = new SerializedVector3(0, 2, 0);
        public SerializedColor errorColor = new SerializedColor(1, 0, 0, 1);
        public KeyCode hold = KeyCode.LeftControl, trigger = KeyCode.T;

        public SerializedVector3 originOffset = new SerializedVector3(0, 1, 0);

        public bool hideMenuTab = false;
        public int verbosity = 2;
    }

    [Serializable]
    public class SerializedColor
    {
        public float red, green, blue, alpha;

        public SerializedColor()
        {
        }

        public SerializedColor(float red, float green, float blue, float alpha)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
        }

        public static implicit operator Color(SerializedColor color)
        {
            return new Color(color.red, color.green, color.blue, color.alpha);
        }
    }

    [Serializable]
    public class SerializedVector3
    {
        public float x, y, z;

        public SerializedVector3()
        {
        }

        public SerializedVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(SerializedVector3 vector3)
        {
            return new Vector3(vector3.x, vector3.y, vector3.z);
        }
    }
}