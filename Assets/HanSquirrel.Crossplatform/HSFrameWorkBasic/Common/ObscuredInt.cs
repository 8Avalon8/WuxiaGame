using System;
using System.Text;

namespace HSFrameWork.Common
{
    public struct ObscuredInt : IEquatable<ObscuredInt>, IFormattable
    {
        private ValueUtils.CoveredInt _CoveredInt;

        #region operators, overrides, interface implementations
        //! @cond
        public static implicit operator ObscuredInt(int value)
        {
            var x = new ObscuredInt();
            x._CoveredInt.Set(value);
            return x;
        }

        public static implicit operator int(ObscuredInt value)
        {
            return value._CoveredInt.Get();
        }

        public static implicit operator ObscuredFloat(ObscuredInt value)
        {
            return value._CoveredInt.Get();
        }

        public static implicit operator ObscuredDouble(ObscuredInt value)
        {
            return value._CoveredInt.Get();
        }

        public static ObscuredInt operator ++(ObscuredInt input)
        {
            input._CoveredInt.Set(input._CoveredInt.Get() + 1);
            return input;
        }

        public static ObscuredInt operator --(ObscuredInt input)
        {
            input._CoveredInt.Set(input._CoveredInt.Get() - 1);
            return input;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// 
        /// <returns>
        /// true if <paramref name="obj"/> is an instance of ObscuredInt and equals the value of this instance; otherwise, false.
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (!(obj is ObscuredInt))
                return false;
            return Equals((ObscuredInt)obj);
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified ObscuredInt value.
        /// </summary>
        /// 
        /// <returns>
        /// true if <paramref name="obj"/> has the same value as this instance; otherwise, false.
        /// </returns>
        /// <param name="obj">An ObscuredInt value to compare to this instance.</param><filterpriority>2</filterpriority>
        public bool Equals(ObscuredInt obj)
        {
            return _CoveredInt.Get() == obj._CoveredInt.Get();
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// 
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return _CoveredInt.Get().GetHashCode();
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// 
        /// <returns>
        /// The string representation of the value of this instance, consisting of a negative sign if the value is negative, and a sequence of digits ranging from 0 to 9 with no leading zeros.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override string ToString()
        {
            return _CoveredInt.Get().ToString();
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation, using the specified format.
        /// </summary>
        /// 
        /// <returns>
        /// The string representation of the value of this instance as specified by <paramref name="format"/>.
        /// </returns>
        /// <param name="format">A numeric format string (see Remarks).</param><exception cref="T:System.FormatException"><paramref name="format"/> is invalid or not supported. </exception><filterpriority>1</filterpriority>

        public string ToString(string format)
        {
            return _CoveredInt.Get().ToString(format);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// 
        /// <returns>
        /// The string representation of the value of this instance as specified by <paramref name="provider"/>.
        /// </returns>
        /// <param name="provider">An <see cref="T:System.IFormatProvider"/> that supplies culture-specific formatting information. </param><filterpriority>1</filterpriority>
        public string ToString(IFormatProvider provider)
        {
            return _CoveredInt.Get().ToString(provider);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation using the specified format and culture-specific format information.
        /// </summary>
        /// 
        /// <returns>
        /// The string representation of the value of this instance as specified by <paramref name="format"/> and <paramref name="provider"/>.
        /// </returns>
        /// <param name="format">A numeric format string (see Remarks).</param><param name="provider">An <see cref="T:System.IFormatProvider"/> that supplies culture-specific formatting information. </param><exception cref="T:System.FormatException"><paramref name="format"/> is invalid or not supported.</exception><filterpriority>1</filterpriority>
        public string ToString(string format, IFormatProvider provider)
        {
            return _CoveredInt.Get().ToString(format, provider);
        }
        //! @endcond
        #endregion
    }
}