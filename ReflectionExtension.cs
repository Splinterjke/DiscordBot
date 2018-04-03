using System;
using System.Reflection;

namespace VoiceOnlineBot
{
    public static class ReflectionExtension
    {
		public static Object GetPropValue(this object obj, string name)
		{
			foreach (String part in name.Split('.'))
			{
				if (obj == null) { return null; }

				Type type = obj.GetType();
				PropertyInfo info = type.GetProperty(part);
				if (info == null) { return null; }

				obj = info.GetValue(obj, null);
			}
			return obj;
		}

		public static T GetPropValue<T>(this object obj, string name)
		{
			Object retval = GetPropValue(obj, name);
			if (retval == null) { return default(T); }

			// throws InvalidCastException if types are incompatible
			return (T)retval;
		}

        public static void SetValue(this object obj, string name, object value)
        {
			PropertyInfo propertyInfo = obj.GetType().GetProperty(name);
			propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType), null);

			// throws InvalidCastException if types are incompatible
        }
    }
}