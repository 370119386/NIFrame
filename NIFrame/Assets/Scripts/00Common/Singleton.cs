namespace NI
{
    public class Singleton<T> where T : Singleton<T>, new()
    {
        protected static T ms_handle = null;
        public static T Instance()
        {
            if (null == ms_handle)
            {
                ms_handle = new T();
            }
            return ms_handle;
        }
    }
}