using UnityEngine;

namespace K
{
    public class OnValueChangeAttribute : PropertyAttribute
    {
        public string methodName;

        public OnValueChangeAttribute(string methodName)
        {
            this.methodName = methodName;
        }
    }
}
