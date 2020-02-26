namespace PyramidNETRS232
{
    using System;

    /// <summary>
    ///     Add-ons for what is missing from 3.5 .NET
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        ///     Returns true if the provided enum values has the specified flag set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Has<T>(this Enum type, T value)
        {
            try
            {
                return ((int) (object) type & (int) (object) value) == (int) (object) value;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Returns true if the specified enum is equal to the specified value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Is<T>(this Enum type, T value)
        {
            try
            {
                return (int) (object) type == (int) (object) value;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Sets the specified flag on this enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Add<T>(this Enum type, T value)
        {
            try
            {
                return (T) (object) ((int) (object) type | (int) (object) value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Could not append value from enumerated type '{typeof(T).Name}'.", ex);
            }
        }

        /// <summary>
        ///     Clears the specified flag on this enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Remove<T>(this Enum type, T value)
        {
            try
            {
                return (T) (object) ((int) (object) type & ~(int) (object) value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Could not remove value from enumerated type '{typeof(T).Name}'.", ex);
            }
        }
    }
}