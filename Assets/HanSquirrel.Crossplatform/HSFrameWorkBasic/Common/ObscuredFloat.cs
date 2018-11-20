using System;
using System.Runtime.InteropServices;
using System.Text;

namespace HSFrameWork.Common
{
    using Inner;
    namespace Inner
    {

        [StructLayout(LayoutKind.Explicit)]
        public struct FloatIntBytesUnion
        {
            [FieldOffset(0)]
            public float f;

            [FieldOffset(0)]
            public int i;

            [FieldOffset(0)]
            public ACTkByte4 b4;
        }

        public struct ACTkByte4
        {
            public byte b1;
            public byte b2;
            public byte b3;
            public byte b4;
        }
    }

    public struct ObscuredFloat : IEquatable<ObscuredFloat>, IFormattable
    {
        private ValueUtils.CoveredInt _CoveredInt;

        private float InnerValue
        {
            get
            {
                var u = new FloatIntBytesUnion();
                u.i = _CoveredInt.Get();
                return u.f;
            }
            set
            {
                var u = new FloatIntBytesUnion();
                u.f = value;
                _CoveredInt.Set(u.i);
            }
        }

        //! @cond
        #region operators, overrides, interface implementations
        public static implicit operator ObscuredFloat(float value)
        {
            var ret = new ObscuredFloat();
            ret.InnerValue = value;
            return ret;
        }

        public static implicit operator float(ObscuredFloat value)
        {
            return value.InnerValue;
        }

        public static ObscuredFloat operator ++(ObscuredFloat input)
        {
            input.InnerValue++;
            return input;
        }

        public static ObscuredFloat operator --(ObscuredFloat input)
        {
            input.InnerValue--;
            return input;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// 
        /// <returns>
        /// true if <paramref name="obj"/> is an instance of ObscuredFloat and equals the value of this instance; otherwise, false.
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (!(obj is ObscuredFloat))
                return false;
            return Equals((ObscuredFloat)obj);
        }

        /// <summary>
        /// Returns a value indicating whether this instance and a specified ObscuredFloat object represent the same value.
        /// </summary>
        /// 
        /// <returns>
        /// true if <paramref name="obj"/> is equal to this instance; otherwise, false.
        /// </returns>
        /// <param name="obj">An ObscuredFloat object to compare to this instance.</param><filterpriority>2</filterpriority>
        public bool Equals(ObscuredFloat obj)
        {
            return obj._CoveredInt.Get() == _CoveredInt.Get();
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
            return InnerValue.GetHashCode();
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// 
        /// <returns>
        /// The string representation of the value of this instance.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override string ToString()
        {
            return InnerValue.ToString();
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation, using the specified format.
        /// </summary>
        /// 
        /// <returns>
        /// The string representation of the value of this instance as specified by <paramref name="format"/>.
        /// </returns>
        /// <param name="format">A numeric format string (see Remarks).</param><exception cref="T:System.FormatException"><paramref name="format"/> is invalid. </exception><filterpriority>1</filterpriority>
        public string ToString(string format)
        {
            return InnerValue.ToString(format);
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
            return InnerValue.ToString(provider);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation using the specified format and culture-specific format information.
        /// </summary>
        /// 
        /// <returns>
        /// The string representation of the value of this instance as specified by <paramref name="format"/> and <paramref name="provider"/>.
        /// </returns>
        /// <param name="format">A numeric format string (see Remarks).</param><param name="provider">An <see cref="T:System.IFormatProvider"/> that supplies culture-specific formatting information. </param><filterpriority>1</filterpriority>
        public string ToString(string format, IFormatProvider provider)
        {
            return InnerValue.ToString(format, provider);
        }

        //! @endcond
        #endregion


    }
}